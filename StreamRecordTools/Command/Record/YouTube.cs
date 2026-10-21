using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ResultType = StreamRecordTools.Program.ResultType;

namespace StreamRecordTools.Command.Record
{
    public static class YouTube
    {
        static string videoId;
        static string fileName;
        static string tempPath;
        static string outputPath;
        static string unarchivedOutputPath;
        static string memberOnlyOutputPath;
        static bool isDisableRedis;
        static DateTime streamScheduledStartTime = DateTime.MinValue;

        public static async Task<ResultType> StartRecord(string id,
            string argOutputPath,
            string argTempPath,
            string argUnArchivedOutputPath,
            string argMemberOnlyOutputPath,
            bool argIsDisableRedis = false,
            bool isDisableLiveFromStart = false,
            bool dontSendStartMessage = false)
        {
            if (string.IsNullOrEmpty(Utility.ToolConfig.GoogleApiKey))
            {
                Log.Error("Google API Key 遺失");
                return ResultType.Error;
            }

            Utility.YouTube = new YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = "StreamRecordTools",
                ApiKey = Utility.ToolConfig.GoogleApiKey,
            });

            string channelId = "", channelTitle = "";
            id = id.Replace("@", "-");
            isDisableRedis = argIsDisableRedis;

            #region 初始化
            if (!isDisableRedis)
            {
                try
                {
                    RedisConnection.Init(Utility.ToolConfig.RedisOption);
                    Utility.Redis = RedisConnection.Instance.ConnectionMultiplexer;
                }
                catch (Exception ex)
                {
                    Log.Error("Redis連線錯誤，請確認伺服器是否已開啟");
                    Log.Error(ex.ToString());
                    return ResultType.Error;
                }
            }

            bool isStartStream = false;
            if (id.Length == 11)
            {
                videoId = id;

                bool isError = false;
                do
                {
                    try
                    {
                        var result = await Utility.GetSnippetDataAndLiveStreamingDetailsByVideoIdAsync(videoId);
                        if (result.VideoSnippet == null)
                        {
                            Log.Error($"{videoId} 不存在直播");
                            return ResultType.Error;
                        }

                        if (result.VideoLiveStreamingDetails.ScheduledStartTimeDateTimeOffset == null)
                        {
                            Log.Error($"{videoId} 無開始直播時間");
                            return ResultType.Error;
                        }

                        channelId = result.VideoSnippet.ChannelId;
                        channelTitle = result.VideoSnippet.ChannelTitle;
                        streamScheduledStartTime = result.VideoLiveStreamingDetails.ScheduledStartTimeDateTimeOffset.Value.LocalDateTime;
                        isStartStream = result.VideoLiveStreamingDetails.ActualStartTimeDateTimeOffset.HasValue;
                        isError = false;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Log.Error(httpEx, "GetSnippetDataAndLiveStreamingDetailsByVideoIdAsync");
                        isError = true;
                        await Task.Delay(1000);
                    }
                    catch (Google.GoogleApiException apiEx) when (apiEx.HttpStatusCode == System.Net.HttpStatusCode.BadRequest && apiEx.Message.Contains("API key not valid"))
                    {
                        Log.Error(apiEx, "GetSnippetDataAndLiveStreamingDetailsByVideoIdAsync: Google API Key 錯誤");
                        return ResultType.Error;
                    }
                } while (isError);
            }
            else
            {
                Log.Error("Video Id 非 11 字元");
                return ResultType.Error;
            }

            Log.Info($"直播影片 Id: {videoId}");
            Log.Info($"頻道 Id: {channelId}");
            Log.Info($"頻道名稱: {channelTitle}");
            Log.Info($"直播預計開始時間: {streamScheduledStartTime}");
            if (!isStartStream)
            {
                do
                {
                    await Task.Delay(1000);
                    if (Utility.IsClose) return ResultType.None;
                } while (streamScheduledStartTime.AddMinutes(-1) > DateTime.Now);
            }
            else
            {
                Log.Warn($"已開台，開始錄影");
            }

            if (!argOutputPath.EndsWith(Utility.GetEnvSlash()))
                argOutputPath += Utility.GetEnvSlash();
            if (!argTempPath.EndsWith(Utility.GetEnvSlash()))
                argTempPath += Utility.GetEnvSlash();
            if (!argUnArchivedOutputPath.EndsWith(Utility.GetEnvSlash()))
                argUnArchivedOutputPath += Utility.GetEnvSlash();
            if (!argMemberOnlyOutputPath.EndsWith(Utility.GetEnvSlash()))
                argMemberOnlyOutputPath += Utility.GetEnvSlash();

            outputPath = argOutputPath.Replace("\"", "");
            tempPath = argTempPath.Replace("\"", "");
            unarchivedOutputPath = argUnArchivedOutputPath.Replace("\"", "");
            memberOnlyOutputPath = argMemberOnlyOutputPath.Replace("\"", "");

            tempPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            outputPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            Log.Info($"輸出路徑: {outputPath}");
            Log.Info($"暫存路徑: {tempPath}");
            Log.Info($"私人存檔路徑: {unarchivedOutputPath}");
            Log.Info($"會限存檔路徑: {memberOnlyOutputPath}");

            if (isDisableLiveFromStart)
                Log.Info("不自動從頭開始錄影");
            #endregion

            bool isCanNotRecordStream = false, isReceiveDownload = false, isNeedRestart = false, isRerecord = false, isHandedOff = false;
            int emptyRerecordCount = 0;
            #region 開始錄製直播
            do
            {
                isNeedRestart = false;

                if (Utility.IsLiveEnd(videoId, !isRerecord, isDisableRedis)) break;

                fileName = $"youtube_{channelId}_{DateTime.Now:yyyyMMdd_HHmmss}_{videoId}";

                CancellationTokenSource cancellationToken = new();
                CancellationToken token = cancellationToken.Token;
                bool isAttemptRecording = false;

                Log.Info($"存檔名稱: {fileName}");
                var process = new Process();
                string browser = "";
                if (OperatingSystem.IsWindows())
                {
                    process.StartInfo.FileName = "yt-dlp_min.exe";
                    browser = "chrome";
                }
                else
                {
                    process.StartInfo.FileName = "yt-dlp";
                    browser = "firefox";
                }

                // 重錄不從頭抓，否則每次都會重複下載整場直播
                foreach (string argument in BuildYtDlpArguments(videoId, $"{tempPath}{fileName}.%(ext)s", isDisableLiveFromStart || isRerecord, Utility.InDocker, browser, onlyIfLive: isRerecord))
                    process.StartInfo.ArgumentList.Add(argument);

                #region 如果過了一小時還沒開始錄影...
                var task = Task.Run(async () =>
                {
                    int waitTime = 3600;
                    do
                    {
                        waitTime--;
                        await Task.Delay(1000);
                        if (token.IsCancellationRequested)
                            return;
                    } while (waitTime >= 0);

                    if (!isReceiveDownload)
                    {
                        var result = await Utility.GetSnippetDataAndLiveStreamingDetailsByVideoIdAsync(videoId);
                        if (result.VideoSnippet == null)
                        {
                            Log.Warn($"{videoId} 待機所已刪除");
                            isCanNotRecordStream = true;
                            process.Kill(Signum.SIGTERM);
                            return;
                        }

                        if (streamScheduledStartTime != result.VideoLiveStreamingDetails.ScheduledStartTimeDateTimeOffset.Value.LocalDateTime)
                        {
                            streamScheduledStartTime = result.VideoLiveStreamingDetails.ScheduledStartTimeDateTimeOffset.Value.LocalDateTime;
                            Log.Info($"已更改開台時間: {streamScheduledStartTime}");
                            isNeedRestart = true;
                            process.Kill(Signum.SIGTERM);
                        }
                        else
                        {
                            Log.Warn("已等待一小時但尚未開始直播，取消錄影");
                            isCanNotRecordStream = true;
                            process.Kill(Signum.SIGTERM);
                        }
                    }
                });
                #endregion

                // 印象中之前用 ErrorDataReceived 的時候能正常觸發會限訊息檢測
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.ErrorDataReceived += (sender, e) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(e.Data))
                            return;

                        // 不顯示 wait 開頭的訊息避免吃爆 Portainer 的 log
                        if (e.Data.ToLower().StartsWith("[wait]"))
                            return;

                        Log.Error(e.Data);

                        if (e.Data.Contains("members-only content") || e.Data.Contains("channel's members"))
                        {
                            Log.Error("檢測到無法讀取的會限");
                            // 旗標要先設：Publish 或 Kill 丟出的例外會被下面的 catch 吃掉，後面的程式碼就跑不到
                            isCanNotRecordStream = true;
                            if (!isDisableRedis)
                                Utility.Redis.GetSubscriber().Publish(new("youtube.startstream", RedisChannel.PatternMode.Literal), $"{videoId}:1");
                            process.Kill(Signum.SIGTERM);
                        }
                        else if (e.Data.Contains("video is private") || e.Data.Contains("Private video"))
                        {
                            Log.Error("已私人化，取消錄影");
                            process.Kill(Signum.SIGTERM);
                            isCanNotRecordStream = true;
                        }
                        else if (e.Data.Contains("video has been removed") || e.Data.Contains("removed by the uploader"))
                        {
                            Log.Error("已移除，取消錄影");
                            process.Kill(Signum.SIGTERM);
                            isCanNotRecordStream = true;
                        }
                    }
                    catch { }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(e.Data))
                            return;

                        // 不顯示 wait 開頭的訊息避免吃爆 Portainer 的 log
                        if (e.Data.ToLower().StartsWith("[wait]"))
                            return;

                        Log.YouTubeInfo(e.Data);
                    }
                    catch { }
                };

                void OnRecordingStarted()
                {
                    if (isReceiveDownload)
                        return;

                    isReceiveDownload = true;
                    Log.Info("開始直播!");

                    if (!isDisableRedis)
                    {
                        if (!dontSendStartMessage) Utility.Redis.GetSubscriber().Publish(new("youtube.startstream", RedisChannel.PatternMode.Literal), $"{videoId}:0");
                        Utility.Redis.GetDatabase().SetAdd("youtube.nowRecord", videoId);

                        if (isDisableLiveFromStart)
                        {
                            #region 每六小時重新執行錄影
                            int waitTime = 5 * 60 * 60 + 59 * 60;
                            var task = Task.Run(async () =>
                            {
                                do
                                {
                                    waitTime--;
                                    await Task.Delay(1000);
                                    if (token.IsCancellationRequested)
                                        return;
                                } while (waitTime >= 0);

                                // 已交給新容器接手，這裡結束後就不要再自動重錄，避免兩邊重複錄同一場
                                isHandedOff = true;
                                Utility.Redis.GetSubscriber().Publish(new("youtube.rerecord", RedisChannel.PatternMode.Literal), videoId);
                            });
                            #endregion
                        }
                    }
                }

                Log.Info($"{process.StartInfo.FileName} {string.Join(' ', process.StartInfo.ArgumentList)}");

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                // 看錄影檔大小而不是 yt-dlp 的輸出格式，換下載器（例如 SABR）時才不用跟著改
                var recordingWatchdogTask = Task.Run(async () =>
                {
                    long lastBytes = 0;
                    long lastChangeTimestamp = Stopwatch.GetTimestamp();
                    try
                    {
                        while (true)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), token);

                            long bytes;
                            try
                            {
                                bytes = GetRecordingBytes(tempPath, fileName);
                            }
                            catch (IOException) // 檔案剛好被 yt-dlp 改名或刪掉，下一輪再算
                            {
                                continue;
                            }

                            if (bytes != lastBytes)
                            {
                                lastBytes = bytes;
                                lastChangeTimestamp = Stopwatch.GetTimestamp();
                                if (bytes > 0 && !isAttemptRecording)
                                {
                                    isAttemptRecording = true;
                                    OnRecordingStarted();
                                }
                                continue;
                            }

                            if (!isAttemptRecording || Stopwatch.GetElapsedTime(lastChangeTimestamp) < TimeSpan.FromMinutes(5))
                                continue;

                            try
                            {
                                if (process.HasExited)
                                    return;

                                process.Kill(entireProcessTree: true);
                            }
                            catch (InvalidOperationException)
                            {
                                return;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "YouTube 錄影逾時，但無法中斷 yt-dlp/ffmpeg");
                                return;
                            }

                            Log.Warn("YouTube 錄影檔超過五分鐘沒有變化，已中斷錄影");
                            return;
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                    }
                });

                process.WaitForExit();
                cancellationToken.Cancel();
                await recordingWatchdogTask;
                process.CancelErrorRead();
                process.CancelOutputRead();

                Log.Info($"錄影結束");

                emptyRerecordCount = isAttemptRecording ? 0 : emptyRerecordCount + 1;
                if (isNeedRestart)
                {
                    do
                    {
                        await Task.Delay(1000);
                        if (Utility.IsClose) break;
                    } while (streamScheduledStartTime.AddMinutes(-1) > DateTime.Now);
                }
                else if (!Utility.IsClose && !isCanNotRecordStream && !isHandedOff && isReceiveDownload && emptyRerecordCount < 10)
                {
                    // yt-dlp 常在直播還沒結束時就退出；等 API 的關台時間跟上，迴圈開頭沒關台就接著錄
                    isRerecord = true;
                    Log.Warn($"yt-dlp 已結束，30 秒後確認是否關台，沒關台就重新錄影 (連續沒錄到資料 {emptyRerecordCount}/10 次)");
                    for (int i = 0; i < 30 && !Utility.IsClose; i++)
                        await Task.Delay(1000);
                }
                else
                {
                    Utility.IsClose = true;
                }
            } while (!Utility.IsClose);
            #endregion

            #region 直播結束後的保存處理
            if (!isCanNotRecordStream) // 如果該直播沒被判定成不能錄影的會限直播的話
            {
                if (!string.IsNullOrEmpty(fileName) && Utility.IsMemberOnly(videoId)) // 如果是會限直播保存到 memberOnlyOutputPath
                {
                    Log.Info($"已轉會限影片，移動資料");
                    MoveVideo(memberOnlyOutputPath, "youtube.memberonly");
                }
                else if (!string.IsNullOrEmpty(fileName) && Utility.IsDelLive) // 如果被刪檔就保存到 unarchivedOutputPath
                {
                    Log.Info($"已刪檔直播，移動資料");
                    MoveVideo(unarchivedOutputPath, "youtube.unarchived");
                }
                else if (Path.GetDirectoryName(outputPath) != Path.GetDirectoryName(tempPath)) // 否則就保存到 outputPath
                {
                    Log.Info("將直播轉移至保存點");
                    MoveVideo(outputPath, "youtube.endstream");

                    if (Utility.IsLiveEnd(videoId, false, isDisableRedis))
                    {
                        // https://social.msdn.microsoft.com/Forums/en-US/c2c12a9f-dc4c-4c9a-b652-65374ef999d8/get-docker-container-id-in-code?forum=aspdotnetcore
                        if (Utility.InDocker && !isDisableRedis)
                            Utility.Redis.GetSubscriber().Publish(new("streamTools.removeById", RedisChannel.PatternMode.Literal), Environment.MachineName);
                    }
                    else
                    {
                        Log.Warn("還沒關台，保留容器以供紀錄檢查");
                    }
                }
            }
            else
            {
                return ResultType.Error;
            }
            #endregion

            if (!isDisableRedis)
                await Utility.Redis.GetDatabase().SetRemoveAsync("youtube.nowRecord", videoId);

            return ResultType.Once;
        }

        public static string[] BuildYtDlpArguments(string videoId, string outputTemplate, bool isDisableLiveFromStart, bool inDocker, string browser, bool onlyIfLive = false)
        {
            // 不指定 -f，用 yt-dlp 預設的 bv*+ba/b；只給 bv 會選到沒有聲音的純影像格式
            // --embed-metadata --embed-thumbnail 會不定時卡住，先不加
            // --mark-watched 疑似會讓 Cookie 出問題，先不加
            List<string> arguments =
            [
                $"https://www.youtube.com/watch?v={videoId}",
                "-o", outputTemplate,
                "--wait-for-video", "15",
            ];

            // --live-from-start 很吃硬碟隨機讀寫
            // 關掉它的話，每六小時要重開一次錄影，不然長時間錄影會遇到 HTTP 503
            // 但從頭開始錄影好像只能錄兩小時 :thinking:
            if (!isDisableLiveFromStart)
                arguments.Add("--live-from-start");

            if (inDocker)
                arguments.AddRange(["--cookies", "/app/cookies.txt"]);
            else
                arguments.AddRange(["--cookies-from-browser", browser]);

            // 重錄時 API 可能還沒顯示關台；直播已結束就跳過，不要改抓存檔
            if (onlyIfLive)
                arguments.AddRange(["--match-filters", "is_live"]);

            return [.. arguments];
        }

        // 只認最終檔，跳過 .mp4.part、.f299.mp4 這類 yt-dlp 中間檔
        public static bool IsRecordedVideoFile(string path, string videoId)
            => Path.GetFileNameWithoutExtension(path).EndsWith(videoId, StringComparison.Ordinal);

        // 含 .part 等中間檔，只算這次錄影（同一個檔名開頭）的檔案
        public static long GetRecordingBytes(string directory, string fileName)
            => new DirectoryInfo(directory).EnumerateFiles($"{fileName}*").Sum(x => x.Length);

        private static void MoveVideo(string outputPath, string redisChannel = "")
        {
            bool isMoved = false;
            foreach (var item in Directory.GetFiles(tempPath, $"*{videoId}.*").Where(x => IsRecordedVideoFile(x, videoId)))
            {
                try
                {
                    Log.Info($"移動 \"{item}\" 至 \"{outputPath}{Path.GetFileName(item)}\"");
                    File.Move(item, $"{outputPath}{Path.GetFileName(item)}");
                    isMoved = true;
                }
                catch (Exception ex)
                {
                    if (Utility.InDocker) Log.Error(ex.ToString());
                    else File.AppendAllText($"{tempPath}{fileName}_err.txt", ex.ToString());
                }
            }

            // 重錄會留下多個檔案，只通知一次，避免 Bot 重複發結束通知
            if (isMoved && !isDisableRedis && !string.IsNullOrEmpty(redisChannel))
                Utility.Redis.GetSubscriber().Publish(new(redisChannel, RedisChannel.PatternMode.Literal), videoId);
        }
    }
}