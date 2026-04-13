# 錄影小幫手

支援 YouTube 及 Twitch 直播錄影，需配置 Google Api Key 以及 YouTube Cookie 使用

若要搭配直播小幫手使用，需要另外安裝 Redis Server 並使用 Subscribe 模式

## 製作 `cookies.txt`

> [!IMPORTANT]
> 優先參考 [yt-dlp 官方說明](https://github.com/yt-dlp/yt-dlp/wiki/Extractors) 來製作

1. 開啟 `不會用到的瀏覽器或無痕模式` 並登入 Youtube (一定要是不常用的，不然 Cookie 會被刷新，本說明以 Chrome 為例)
2. 下載 `ChromeCookiesView` ([官網](https://www.nirsoft.net/utils/chrome_cookies_view.html), [直接下載](https://www.nirsoft.net/utils/chromecookiesview.zip))
3. 解壓縮並開啟 `ChromeCookiesView.exe`
4. 搜尋 `.youtube.com` 域名相關 Cookie
5. 將所有搜尋到的 Cookie 複製成 `Netscape Cookie` 格式 (Copy As Cookies.txt Format)
6. 建立 `cookies.txt` 並將 Cookie 貼上

## 直接執行程式

需要從頭將專案編譯，我相信你可以自己搞定參數設定及如何開始錄影的

## Docker 環境，Sub 模式

本模式是設計給直播小幫手串接使用，一般無需使用

1. 複製專案 `git clone https://github.com/konnokai/StreamRecordTools.git`
2. 開啟 `.env_sample` 編輯為正確設定值後存檔為 `.env` 到專案目錄內
 **\*請務必確定所有路徑皆為絕對路徑\***
3. 部屬 Docker Image `docker compose up -d`

### Redis 頻道

使用 Redis Publish 指令觸發錄影：

| 頻道 | 參數 | 說明 |
|------|------|------|
| `youtube.record` | 11 碼 VideoId | 觸發 YouTube 直播錄影 |
| `twitch.record` | Twitch UserLogin | 觸發 Twitch 直播錄影 |

### 環境變數說明

| 變數名稱 | 必填 | 說明 |
|----------|------|------|
| `GoogleApiKey` | ✅ | Google API 金鑰 |
| `RedisOption` | ✅ | Redis 連線設定 |
| `UptimeKumaPushUrl` | ❌ | Uptime Kuma Push 監視器網址 |
| `RecordPath` | ✅ | YouTube & Twitch 直播存檔路徑（絕對路徑） |
| `TempPath` | ✅ | 錄影暫存路徑（絕對路徑） |
| `YouTubeUnarchivedPath` | ✅ | YouTube 刪檔直播保存路徑（絕對路徑） |
| `TwitchUnarchivedPath` | ✅ | Twitch 刪檔直播保存路徑（絕對路徑） |
| `MemberOnlyPath` | ✅ | YouTube 會限直播保存路徑（絕對路徑） |
| `TwitchUnarchivedUserLogins` | ❌ | 需自動保存至刪檔直播資料夾的 Twitch UserLogin 清單，JSON Array 格式，例如：`["user1","user2"]` |
| `TwitchCookieAuthToken` | ❌ | Twitch Cookie Auth Token，請參考 [Streamlink 說明](https://streamlink.github.io/cli/plugins/twitch.html#authentication) |
| `CookiesFilePath` | ✅ | YouTube Cookie 檔案路徑（絕對路徑，Netscape 格式） |

## Docker 環境，單一直播錄影模式

1. 複製專案 `git clone https://github.com/konnokai/StreamRecordTools.git` (或是單獨下載 `.env_sample` 並放到新資料夾)
2. `cd StreamRecordTools`
3. 根據上方說明製作 `cookies.txt` 並將文件放置專案目錄
4. 開啟 `.env_sample` 並編輯 `GoogleApiKey` 成正確的 ApiKey 後存檔為 `.env` 到專案目錄內

### YouTube

取得 11 碼的 VideoId 並替換下方指令中的 `(VideoId)` 區塊

```bash
docker run -it -d --env-file .env \
  -v "/record/output:/output" \
  -v "/record/temp:/temp_path" \
  -v "/record/youtube_unarchived:/unarchived" \
  -v "/record/member_only:/member_only" \
  -v "/cookies.txt:/app/cookies.txt" \
  jun112561/stream-record-tools:master yt_once_on_docker (VideoId) -d -s
```

### Twitch

取得 Twitch 實況主的 UserLogin 並替換下方指令中的 `(UserLogin)` 區塊

```bash
docker run -it -d --env-file .env \
  -v "/record/output:/output" \
  -v "/record/temp:/temp_path" \
  -v "/record/twitch_unarchived:/twitch_unarchived" \
  jun112561/stream-record-tools:master twitch_once (UserLogin) -o /output -t /temp_path -u /twitch_unarchived
```

> [!NOTE]
> Docker `-v` 參數請自行替換成實體主機中要保存的絕對路徑，唯獨 Container 掛載路徑不可變更

若需要從頭開始錄影請將指令最後面的 `-s` 移除

> [!WARNING]
> 從頭開始直播僅可從頭錄影兩小時，無法超過兩小時，尚不確定是 yt-dlp 問題還是 YouTube 限制，非特殊情況建議不要從頭開始錄影
