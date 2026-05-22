using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.IO;
using TwitchLib.Api;
using static StreamRecordTools.Program;
using ResultType = StreamRecordTools.Program.ResultType;

namespace StreamRecordTools.Command.Record
{
    public class Twitch
    {
        static string userLogin;
        static string fileName;
        static string tempPath;
        static string unarchivedOutputPath;
        static string outputPath;
        static bool isDisableRedis;
        static TwitchAPI twitchApi;

        static readonly string _twitchOAuthToken = Utility.ToolConfig.TwitchCookieAuthToken;

        public static ResultType StartRecord(TwitchOnceOptions options)
        {
            isDisableRedis = options.DisableRedis;

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

            userLogin = options.UserLogin;
            fileName = $"[{userLogin}] - {DateTime.Now:yyyyMMdd_HHmmss}.ts";

            if (options.IsSaveToUnarchived)
            {
                if (string.IsNullOrEmpty(Utility.ToolConfig.TwitchClientId) || string.IsNullOrEmpty(Utility.ToolConfig.TwitchClientSecret))
                {
                    Log.Warn($"{nameof(Utility.ToolConfig.TwitchClientId)} 或 {nameof(Utility.ToolConfig.TwitchClientSecret)} 遺失，無法獲取標題");
                    return ResultType.Error;
                }

                twitchApi = new()
                {
                    Helix =
                    {
                        Settings =
                        {
                            ClientId = Utility.ToolConfig.TwitchClientId,
                            Secret = Utility.ToolConfig.TwitchClientSecret
                        }
                    }
                };

                var streamsResponse = twitchApi.Helix.Streams.GetStreamsAsync(first: 3, userLogins: [userLogin]).GetAwaiter().GetResult();
                if (streamsResponse != null)
                {
                    var stream = streamsResponse.Streams[0] ?? null;
                    if (stream != null)
                    {
                        Log.Info($"Twitch 分類: {stream.GameName}");
                        Log.Info($"Twitch 標題: {Utility.ToSafeFilename(stream.Title)}");
                        var title = Utility.ToSafeFilename(stream.Title);
                        fileName = $"[{DateTime.Now:yyyyMMdd-HHmmss}] ({stream.GameName}) - {title.Substring(0, Math.Min(title.Length, 150))} - {stream.Id}.ts";
                    }
                }
            }

            if (!options.OutputPath.EndsWith(Utility.GetEnvSlash()))
                options.OutputPath += Utility.GetEnvSlash();
            if (!options.TempPath.EndsWith(Utility.GetEnvSlash()))
                options.TempPath += Utility.GetEnvSlash();
            if (!options.TwitchUnarchivedOutputPath.EndsWith(Utility.GetEnvSlash()))
                options.TwitchUnarchivedOutputPath += Utility.GetEnvSlash();

            outputPath = options.OutputPath.Replace("\"", "").Trim();
            tempPath = options.TempPath.Replace("\"", "").Trim();
            unarchivedOutputPath = options.TwitchUnarchivedOutputPath.Replace("\"", "").Trim();

            tempPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            outputPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            unarchivedOutputPath += $"{userLogin}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(unarchivedOutputPath) && options.IsSaveToUnarchived) Directory.CreateDirectory(unarchivedOutputPath);

            Log.Info($"輸出路徑: {outputPath}");
            Log.Info($"暫存路徑: {tempPath}");
            Log.Info($"私人存檔路徑: {unarchivedOutputPath}");

            string procArgs = $"--progress no --output \"{tempPath}{fileName}\"";
            if (!string.IsNullOrEmpty(_twitchOAuthToken) && _twitchOAuthToken.Length == 30)
                procArgs += $" \"--twitch-api-header=Authorization=OAuth {_twitchOAuthToken}\"";

            procArgs += $" https://twitch.tv/{userLogin} best";

            var process = new Process();
            process.StartInfo.FileName = "streamlink";
            process.StartInfo.Arguments = procArgs;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.ErrorDataReceived += (sender, e) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;

                    Log.Error(e.Data);
                }
                catch { }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;

                    Log.YouTubeInfo(e.Data);
                }
                catch { }
            };

            Log.Info(process.StartInfo.Arguments);

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
            process.CancelErrorRead();
            process.CancelOutputRead();

            if (options.IsSaveToUnarchived)
            {
                Log.Info("將直播轉移至私人存檔");
                MoveVideo(unarchivedOutputPath);
            }
            else if (Path.GetDirectoryName(outputPath) != Path.GetDirectoryName(tempPath))
            {
                Log.Info("將直播轉移至保存點");
                MoveVideo(outputPath);
            }

            // https://social.msdn.microsoft.com/Forums/en-US/c2c12a9f-dc4c-4c9a-b652-65374ef999d8/get-docker-container-id-in-code?forum=aspdotnetcore
            if (Utility.InDocker && !isDisableRedis)
                Utility.Redis.GetSubscriber().Publish(new("streamTools.removeById", RedisChannel.PatternMode.Literal), Environment.MachineName);

            return ResultType.Once;
        }

        private static void MoveVideo(string outputPath)
        {
            try
            {
                Log.Info($"移動 \"{tempPath}{fileName}\" 至 \"{outputPath}{fileName}\"");
                File.Move($"{tempPath}{fileName}", $"{outputPath}{fileName}");
            }
            catch (Exception ex)
            {
                if (Utility.InDocker) Log.Error(ex.ToString());
                else File.AppendAllText($"{tempPath}{fileName}_err.txt", ex.ToString());
            }
        }
    }
}
