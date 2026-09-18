using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using static StreamRecordTools.Program;
using ResultType = StreamRecordTools.Program.ResultType;

namespace StreamRecordTools.Command.Record
{
    /// <summary>
    /// CHZZK 單場錄影：以 Streamlink 錄製 <c>https://chzzk.naver.com/live/{channelId}</c>，
    /// 先寫暫存目錄，結束後依結果搬到 CHZZK 保存目錄。
    /// <para>
    /// 立即錄影與自動錄影可能同時抵達，啟動 Streamlink 前先取得頻道層級的 Redis 執行鎖
    /// （StackExchange.Redis <c>LockTake</c> / <c>LockExtend</c> / <c>LockRelease</c>），
    /// 同頻道同一時間只執行一份 Streamlink；鎖有 TTL，程序崩潰後會自動到期，不會永久卡住。
    /// </para>
    /// </summary>
    public class Chzzk
    {
        /// <summary>
        /// 頻道鎖 TTL。依據：既有 RedisOption 使用 <c>syncTimeout=3000</c>（毫秒），
        /// 續租週期 20 秒時，連續兩次續租失敗仍不會超過 60 秒 TTL；第三次失敗前鎖已可被其他工作取得，
        /// 不會出現「沒有鎖卻持續錄影」的長時間重疊。此為執行互斥，不禁止工作結束後重新委派。
        /// </summary>
        private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(60);

        /// <summary>續租週期；需明顯小於 <see cref="LockTtl"/>。</summary>
        private static readonly TimeSpan LockRenewInterval = TimeSpan.FromSeconds(20);

        private static readonly Regex ChannelIdPattern = new(
            "^[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex StreamKeyPattern = new(
            "^[0-9a-f]{32}:[0-9]{8}_[0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>錄影收尾結果；非 0 退出碼但有檔案時保留部分資料，不一律視為成功。</summary>
        public enum RecordOutcome
        {
            /// <summary>正常結束且檔案非空。</summary>
            Completed,

            /// <summary>程序退出碼非 0 或失去鎖，但已保留非空檔案。</summary>
            Partial,

            /// <summary>沒有可保留的檔案。</summary>
            Failed
        }

        private sealed class ChzzkRecordPayload
        {
            [JsonProperty("channelId")]
            public string ChannelId { get; set; }

            [JsonProperty("streamKey")]
            public string StreamKey { get; set; }
        }

        private sealed class LockState
        {
            public int Lost;
        }

        public static ResultType StartRecord(ChzzkOnceOptions options)
        {
            string channelId = options.ChannelId?.Trim();
            string streamKey = options.StreamKey?.Trim();
            if (!TryValidate(channelId, streamKey))
            {
                Log.Error($"CHZZK 錄影參數格式錯誤，頻道 ID 需為 32 位十六進位，場次鍵格式為 channelId:yyyyMMdd_HHmmss");
                return ResultType.Error;
            }

            IDatabase redis = null;
            ConnectionMultiplexer redisConnection = null;
            if (!options.DisableRedis)
            {
                try
                {
                    RedisConnection.Init(Utility.ToolConfig.RedisOption);
                    redisConnection = RedisConnection.Instance.ConnectionMultiplexer;
                    redis = redisConnection.GetDatabase();
                }
                catch (Exception ex)
                {
                    Log.Error("Redis連線錯誤，請確認伺服器是否已開啟");
                    Log.Error(ex.ToString());
                    return ResultType.Error;
                }
            }
            else
            {
                // ponytail: --disable-redis 不提供跨程序互斥（同 TwitCasting 的單機模式），需要互斥時使用訂閱派工。
                Log.Warn("已停用 Redis，本次錄影不提供跨程序互斥");
            }

            string outputPath = EnsureTrailingSlash(Utility.ToolConfig.ChzzkRecordPath);
            string tempPath = EnsureTrailingSlash(options.TempPath).Replace("\"", "").Trim();
            outputPath = outputPath.Replace("\"", "").Trim();

            tempPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            outputPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            string baseFileName = $"chzzk_{channelId}_{GetStreamKeyTimestamp(streamKey)}";
            string outputFile = CreateUniqueFilePath(tempPath, baseFileName, ".ts");

            Log.Info($"輸出路徑: {outputPath}");
            Log.Info($"暫存路徑: {tempPath}");

            RedisValue lockToken = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
            bool hasLock = false;
            if (redis != null)
            {
                try
                {
                    hasLock = redis.LockTake(LockKey(channelId), lockToken, LockTtl);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                if (!hasLock)
                {
                    Log.Warn($"CHZZK 頻道已有錄影工作持有執行鎖，本次請求略過: {channelId}");
                    return ResultType.Error;
                }
            }

            var lockState = new LockState();
            Timer renewTimer = null;
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "streamlink",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };
                foreach (string argument in BuildStreamlinkArguments(outputFile, channelId))
                    process.StartInfo.ArgumentList.Add(argument);

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Log.Error(e.Data);
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Log.YouTubeInfo(e.Data);
                };

                Log.Info($"streamlink {string.Join(' ', process.StartInfo.ArgumentList)}");

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    Log.Error($"無法啟動 Streamlink，請確認錄影環境已安裝: {channelId}");
                    return ResultType.Error;
                }

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                if (redis != null)
                {
                    renewTimer = new Timer(_ =>
                    {
                        try
                        {
                            if (!redis.LockExtend(LockKey(channelId), lockToken, LockTtl))
                            {
                                MarkLockLost(process, lockState, "執行鎖已失效");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Redis 中斷時無法續租；到期後鎖可能被其他工作取得，因此視同失去鎖並停止錄影。
                            Log.Error(ex.ToString());
                            MarkLockLost(process, lockState, "無法續租執行鎖");
                        }
                    }, null, LockRenewInterval, LockRenewInterval);
                }

                process.WaitForExit();
                process.CancelErrorRead();
                process.CancelOutputRead();

                int exitCode = process.ExitCode;
                bool fileExists = File.Exists(outputFile);
                long fileLength = fileExists ? new FileInfo(outputFile).Length : 0;
                RecordOutcome outcome = DecideOutcome(exitCode, fileExists, fileLength);

                if (outcome == RecordOutcome.Failed)
                {
                    Log.Error($"CHZZK 錄影未產生可用檔案 (退出碼 {exitCode})，暫存路徑: {outputFile}");
                }
                else if (outcome == RecordOutcome.Partial)
                {
                    Log.Warn($"CHZZK 錄影非正常結束 (退出碼 {exitCode}{(Volatile.Read(ref lockState.Lost) == 1 ? "、已失去執行鎖" : "")})，保留已下載內容: {outputFile}");
                }
                else
                {
                    Log.Info($"CHZZK 錄影結束 (退出碼 {exitCode})，檔案大小: {fileLength} bytes");
                }

                if (outcome != RecordOutcome.Failed &&
                    Path.GetDirectoryName(outputPath) != Path.GetDirectoryName(tempPath))
                {
                    Log.Info("將直播轉移至保存點");
                    MoveVideo(outputFile, Path.Combine(outputPath, Path.GetFileName(outputFile)));
                }

                if (Utility.InDocker && !options.DisableRedis)
                    redisConnection?.GetSubscriber().Publish(new("streamTools.removeById", RedisChannel.PatternMode.Literal), Environment.MachineName);

                return outcome == RecordOutcome.Failed ? ResultType.Error : ResultType.Once;
            }
            finally
            {
                renewTimer?.Dispose();
                if (hasLock)
                {
                    try
                    {
                        if (!redis.LockRelease(LockKey(channelId), lockToken))
                            Log.Warn($"CHZZK 執行鎖已非本程序持有，略過釋放: {channelId}");
                    }
                    catch (Exception ex)
                    {
                        // TTL 會讓鎖自然到期；釋放失敗不影響已完成的檔案保存。
                        Log.Error(ex.ToString());
                    }
                }
            }
        }

        private static void MarkLockLost(Process process, LockState lockState, string reason)
        {
            if (Interlocked.Exchange(ref lockState.Lost, 1) == 1)
                return;

            Log.Error($"CHZZK 錄影失去執行鎖（{reason}），終止 Streamlink 並保留已下載檔案");
            try
            {
                if (!process.HasExited)
                    process.Kill(Signum.SIGTERM);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        /// <summary>解析並驗證 <c>chzzk.record</c> payload；只接受本工具認可的欄位，不接受任意 URL、命令或路徑。</summary>
        public static bool TryParseRequest(string payload, out string channelId, out string streamKey)
        {
            channelId = null;
            streamKey = null;
            if (string.IsNullOrWhiteSpace(payload))
                return false;

            ChzzkRecordPayload request;
            try
            {
                request = JsonConvert.DeserializeObject<ChzzkRecordPayload>(payload);
            }
            catch (JsonException)
            {
                return false;
            }

            if (request == null || !TryValidate(request.ChannelId, request.StreamKey))
                return false;

            channelId = request.ChannelId;
            streamKey = request.StreamKey;
            return true;
        }

        /// <summary>驗證頻道 ID 與場次鍵格式，且場次鍵必須以同一頻道 ID 開頭。</summary>
        public static bool TryValidate(string channelId, string streamKey)
            => !string.IsNullOrWhiteSpace(channelId) && !string.IsNullOrWhiteSpace(streamKey) &&
                ChannelIdPattern.IsMatch(channelId) && StreamKeyPattern.IsMatch(streamKey) &&
                streamKey.StartsWith(channelId + ":", StringComparison.Ordinal);

        /// <summary>由場次鍵取出檔名用的開台時間（yyyyMMdd_HHmmss）；場次鍵含冒號，不能直接用於 Windows 檔名。</summary>
        public static string GetStreamKeyTimestamp(string streamKey)
        {
            int separator = streamKey?.IndexOf(':') ?? -1;
            return separator >= 0 ? streamKey[(separator + 1)..] : streamKey;
        }

        /// <summary>Streamlink 參數；使用程序參數 API 傳值，沿用預設 <c>best</c> 與受控輸出路徑，不經 shell。</summary>
        public static string[] BuildStreamlinkArguments(string outputFile, string channelId)
            => ["--progress", "no", "--output", outputFile, $"https://chzzk.naver.com/live/{channelId}", "best"];

        /// <summary>建立不覆蓋既有檔案的輸出路徑；同場重錄時序號遞增。</summary>
        public static string CreateUniqueFilePath(string directory, string baseFileName, string extension)
        {
            string path = Path.Combine(directory, baseFileName + extension);
            for (int index = 2; File.Exists(path); index++)
                path = Path.Combine(directory, $"{baseFileName}_{index}{extension}");
            return path;
        }

        /// <summary>依退出碼與檔案狀態決定收尾；沒有非空檔案一律視為失敗，不記錄成成功。</summary>
        public static RecordOutcome DecideOutcome(int exitCode, bool fileExists, long fileLength)
        {
            if (fileExists && fileLength > 0)
                return exitCode == 0 ? RecordOutcome.Completed : RecordOutcome.Partial;
            return RecordOutcome.Failed;
        }

        private static string LockKey(string channelId) => $"chzzk:record:lock:{channelId}";

        private static string EnsureTrailingSlash(string path)
        {
            if (!path.EndsWith(Utility.GetEnvSlash()))
                path += Utility.GetEnvSlash();
            return path;
        }

        private static void MoveVideo(string sourceFile, string targetFile)
        {
            try
            {
                if (string.Equals(sourceFile, targetFile, StringComparison.Ordinal))
                    return;

                File.Move(sourceFile, targetFile);
            }
            catch (Exception ex)
            {
                // 搬檔失敗仍保留暫存檔，並在檔名旁留下錯誤資訊。
                if (Utility.InDocker) Log.Error(ex.ToString());
                else File.AppendAllText($"{sourceFile}_err.txt", ex.ToString());
                Log.Error($"搬移錄影檔失敗，已保留暫存檔: {sourceFile}");
            }
        }
    }
}
