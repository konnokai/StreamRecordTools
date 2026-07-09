using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.IO;
using static StreamRecordTools.Program;
using ResultType = StreamRecordTools.Program.ResultType;

namespace StreamRecordTools.Command.Record
{
    public class Twitcasting
    {
        static string channelId;
        static string fileName;
        static string tempPath;
        static string outputPath;
        static bool isDisableRedis;

        public static ResultType StartRecord(TwitcastingOnceOptions options)
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

            channelId = options.ChannelId;
            fileName = $"[{channelId}] - {DateTime.Now:yyyyMMdd_HHmmss}.ts";

            if (!options.OutputPath.EndsWith(Utility.GetEnvSlash()))
                options.OutputPath += Utility.GetEnvSlash();
            if (!options.TempPath.EndsWith(Utility.GetEnvSlash()))
                options.TempPath += Utility.GetEnvSlash();

            outputPath = options.OutputPath.Replace("\"", "").Trim();
            tempPath = options.TempPath.Replace("\"", "").Trim();

            tempPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            outputPath += $"{DateTime.Now:yyyyMMdd}{Utility.GetEnvSlash()}";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            Log.Info($"輸出路徑: {outputPath}");
            Log.Info($"暫存路徑: {tempPath}");

            // TwitCasting 無需 OAuth header，直接沿用 streamlink 錄影
            string procArgs = $"--progress no --output \"{tempPath}{fileName}\" https://twitcasting.tv/{channelId} best";

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

            if (Path.GetDirectoryName(outputPath) != Path.GetDirectoryName(tempPath))
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
