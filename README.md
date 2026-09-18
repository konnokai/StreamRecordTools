# 錄影小幫手

錄影小幫手可錄製 YouTube、Twitch 與 TwitCasting 直播。它可以單次執行，也可以訂閱 Redis 頻道，接收其他服務送出的錄影工作。

## 建議使用方式

- 只錄一場直播：使用單次 Docker 指令，不需要 Redis。
- 搭配直播小幫手自動錄影：使用 Docker Compose 的訂閱模式，需要 Redis。
- 修改或除錯程式：安裝 .NET 10 SDK 後直接執行。

## 系統需求

- Docker，或 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 已啟用 YouTube Data API v3 的 Google API Key
- YouTube 錄影需要 `cookies.txt`
- 訂閱模式需要 Redis
- 不使用 Docker 時，需自行安裝 `yt-dlp`、`ffmpeg` 與 `streamlink`，並確認可從 `PATH` 執行

## 準備 YouTube Cookie

請依 [yt-dlp 官方說明](https://github.com/yt-dlp/yt-dlp/wiki/Extractors) 匯出 Netscape 格式的 `cookies.txt`。建議使用專門給錄影工具的瀏覽器設定檔或帳號，避免日常登入登出讓 Cookie 提前失效。

`cookies.txt` 等同登入憑證：

- 不要提交到 Git。
- 不要傳給其他人。
- 檔案權限只開放給執行錄影工具的帳號。

## Docker Compose 訂閱模式

1. 複製環境變數範例：

```powershell
Copy-Item .env_sample .env
```

Linux 或 macOS：

```sh
cp .env_sample .env
```

2. 編輯 `.env`，填入 Google API Key、Redis 與錄影路徑。
3. 將 `CookiesFilePath` 設為主機上 `cookies.txt` 的絕對路徑。
4. 啟動服務：

```sh
docker compose up -d
docker compose logs -f stream-record-master
```

Compose 會掛載 Docker socket，讓主服務建立單次錄影容器。只有信任的程式與使用者可以存取這個服務，因為 Docker socket 等同主機管理權限。

### Redis 錄影頻道

| 頻道 | 訊息內容 | 用途 |
|---|---|---|
| `youtube.record` | 11 碼 YouTube Video ID | 開始錄製 YouTube 直播 |
| `twitch.record` | Twitch UserLogin | 開始錄製 Twitch 直播 |
| `twitcasting.record` | TwitCasting screen ID | 開始錄製 TwitCasting 直播 |

例如：

```sh
redis-cli PUBLISH youtube.record dQw4w9WgXcQ
```

## Docker 單次錄影

下列路徑都要換成主機上的絕對路徑。

### YouTube

```sh
docker run --rm --env-file .env \
  -v "/record/output:/output" \
  -v "/record/temp:/temp_path" \
  -v "/record/youtube_unarchived:/unarchived" \
  -v "/record/member_only:/member_only" \
  -v "/record/cookies.txt:/app/cookies.txt:ro" \
  jun112561/stream-record-tools:master \
  yt_once_on_docker VIDEO_ID -d -s
```

`-d` 表示不使用 Redis。`-s` 表示從目前時間開始錄影；移除 `-s` 會嘗試從直播開頭錄製，但平台不保證能取得完整內容。

### Twitch

```sh
docker run --rm --env-file .env \
  -v "/record/output:/output" \
  -v "/record/temp:/temp_path" \
  -v "/record/twitch_unarchived:/twitch_unarchived" \
  jun112561/stream-record-tools:master \
  twitch_once USER_LOGIN -o /output -t /temp_path -u /twitch_unarchived -d
```

## 直接使用 .NET 執行

第一次執行時，程式會產生 `tool_config_example.json` 後退出。將它複製成 `tool_config.json`，填入 `GoogleApiKey` 與需要的設定，再重新執行。

建置：

```powershell
dotnet build StreamRecordTools.sln -c Release
```

錄製 YouTube：

```powershell
dotnet run -c Release --project StreamRecordTools -- yt_once VIDEO_ID -o D:\record -t D:\temp -u D:\unarchived -m D:\member-only -d -s
```

錄製 Twitch：

```powershell
dotnet run -c Release --project StreamRecordTools -- twitch_once USER_LOGIN -o D:\record -t D:\temp -u D:\twitch-unarchived -d
```

查看所有參數：

```powershell
dotnet run --project StreamRecordTools -- --help
```

## 環境變數

| 變數 | 用途 |
|---|---|
| `GoogleApiKey` | YouTube Data API v3 金鑰 |
| `RedisOption` | Redis 連線設定 |
| `UptimeKumaPushUrl` | 選用的 Uptime Kuma Push URL |
| `RecordPath` | 一般錄影輸出路徑 |
| `TwitcastingRecordPath` | TwitCasting 輸出路徑 |
| `TempPath` | 暫存路徑 |
| `YouTubeUnarchivedPath` | YouTube 刪檔或私人直播保存路徑 |
| `TwitchUnarchivedPath` | Twitch 永久保存路徑 |
| `MemberOnlyPath` | YouTube 會員限定直播保存路徑 |
| `TwitchUnarchivedUserLogins` | 永久保存的 Twitch UserLogin JSON 陣列 |
| `TwitchClientId`、`TwitchClientSecret` | 查詢 Twitch 實況資訊時使用 |
| `TwitchCookieAuthToken` | Streamlink 的 Twitch Cookie Auth Token |
| `CookiesFilePath` | Netscape 格式 `cookies.txt` 的絕對路徑 |

路徑不存在、空間不足、Cookie 失效或平台限制都可能讓錄影中斷。重要直播請先用測試頻道確認設定與磁碟空間。
