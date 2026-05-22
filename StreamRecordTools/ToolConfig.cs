using Newtonsoft.Json;
using StreamRecordTools;
using System;
using System.IO;

public class ToolConfig
{
    public string GoogleApiKey { get; set; } = "";
    public string RedisOption { get; set; } = "127.0.0.1,syncTimeout=3000";
    public string UptimeKumaPushUrl { get; set; } = "";
    public string RecordPath { get; set; } = "./record";
    public string TempPath { get; set; } = "/tmp";
    public string MemberOnlyPath { get; set; } = "./member_only";
    public string YouTubeUnarchivedPath { get; set; } = "./youtube_unarchived";
    public string TwitchUnarchivedPath { get; set; } = "./twitch_unarchived";
    public string TwitchUnarchivedUserLogins { get; set; } = "[]";
    public string TwitchCookieAuthToken { get; set; } = "";
    public string CookiesFilePath { get; set; } = "./cookies.txt";

    public void InitBotConfig()
    {
        if (Utility.InDocker)
        {
            Log.Info("從環境變數讀取設定");

            foreach (var item in GetType().GetProperties())
            {
                bool exitIfNoVar = false;
                object origValue = item.GetValue(this);
                if (origValue == default) exitIfNoVar = true;

                object setValue = Utility.GetEnvironmentVariable(item.Name, item.PropertyType, exitIfNoVar);
                if (setValue == null) setValue = origValue;

                item.SetValue(this, setValue);
            }
        }
        else
        {
            try { File.WriteAllText("tool_config_example.json", JsonConvert.SerializeObject(new ToolConfig(), Formatting.Indented)); } catch { }
            if (!File.Exists("tool_config.json"))
            {
                Log.Error($"tool_config.json 遺失，請依照 {Path.GetFullPath("tool_config_example.json")} 內的格式填入正確的數值");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            var config = JsonConvert.DeserializeObject<ToolConfig>(File.ReadAllText("tool_config.json"));

            try
            {
                if (string.IsNullOrWhiteSpace(config.GoogleApiKey))
                {
                    Log.Error($"{nameof(GoogleApiKey)} 遺失，請輸入至 tool_config.json 後重開程式");
                    if (!Console.IsInputRedirected)
                        Console.ReadKey();
                    Environment.Exit(3);
                }

                GoogleApiKey = config.GoogleApiKey;
                RedisOption = config.RedisOption;
                UptimeKumaPushUrl = config.UptimeKumaPushUrl;
                RecordPath = config.RecordPath;
                TempPath = config.TempPath;
                MemberOnlyPath = config.MemberOnlyPath;
                YouTubeUnarchivedPath = config.YouTubeUnarchivedPath;
                TwitchUnarchivedPath = config.TwitchUnarchivedPath;
                TwitchUnarchivedUserLogins = config.TwitchUnarchivedUserLogins;
                TwitchCookieAuthToken = config.TwitchCookieAuthToken;
                CookiesFilePath = config.CookiesFilePath;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
    }
}