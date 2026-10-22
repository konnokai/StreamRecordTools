# 錄影小幫手

錄影小幫手可錄製 YouTube、Twitch、TwitCasting 與 CHZZK 直播。它可以單次執行，也可以訂閱 Redis 頻道，接收其他服務送出的錄影工作。

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

Compose 也會啟動 `bgutil-provider`，提供 YouTube 需要的 PO Token。YouTube 錄影容器透過 `stream-record-tools` 網路，用 `http://bgutil-provider:4416` 連線。`bgutil-provider` 的版本要和 image 內的 plugin 一致，目前都是 `2.0.2`。

### YouTube 錄影方式

Docker image 內的 yt-dlp 是 [bashonly 的 SABR 測試版](https://github.com/bashonly/yt-dlp/releases/tag/sabr)。原因是 YouTube 常在直播還沒結束時，就先結束 HLS 播放清單；SABR 能錄到真正關台。

- 輸出檔是 `.mkv`（VP9 影像 + AAC 聲音）。
- 拿不到 SABR 格式時，會自動改用 HLS 錄影，輸出可能是 `.mp4`。
- 直接用 .NET 執行時，用的是你自己安裝的 yt-dlp，不會套用以上設定。

### Redis 錄影頻道

| 頻道 | 訊息內容 | 用途 |
|---|---|---|
| `youtube.record` | 11 碼 YouTube Video ID | 開始錄製 YouTube 直播 |
| `twitch.record` | Twitch UserLogin | 開始錄製 Twitch 直播 |
| `twitcasting.record` | TwitCasting screen ID | 開始錄製 TwitCasting 直播 |
| `chzzk.record` | JSON，含 `channelId` 與 `streamKey` | 開始錄製 CHZZK 直播 |

例如：

```sh
redis-cli PUBLISH youtube.record dQw4w9WgXcQ
```

### 每日存檔檢查

訂閱模式每天 00:00 會掃描錄影保存路徑內的日期資料夾：

- API 查不到、觀看頁也確認是私人或已刪除的影片，移到 `YouTubeUnarchivedPath`，並送出 `youtube.unarchived`。
- 錄完後才轉成會限的影片，移到 `MemberOnlyPath`，並送出 `youtube.memberonly.late`。

CHZZK 的訊息格式如下。`streamKey` 為 `channelId:yyyyMMdd_HHmmss`，只作為檔名與日誌追蹤；實際錄影使用頻道直播網址。

```json
{
  "channelId": "4de764d9dad3b25602284be6db3ac647",
  "streamKey": "4de764d9dad3b25602284be6db3ac647:20260918_120000"
}
```

同一個頻道的錄影工作以 Redis 鎖互斥，手動與自動請求同時抵達時只會執行一份 Streamlink。鎖有到期時間，程序意外結束後會自動恢復，不會永久卡住。使用單次 CLI 的 `--disable-redis` 時不提供這項互斥。

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

錄影容器會連 `http://bgutil-provider:4416` 取得 PO Token。單次執行時，要先在同一個 Docker 網路啟動 bgutil，並在上面的指令加上 `--network yt-record`：

```sh
docker network create yt-record
docker run -d --name bgutil-provider --network yt-record --init brainicism/bgutil-ytdlp-pot-provider:2.0.2
```

沒有 bgutil 時，能不能錄影要看帳號狀態，這種情況還沒測過。

錄影開始後，如果 yt-dlp 提前結束，或錄影檔超過五分鐘沒有變化，但 API 顯示直播還沒結束，程式會等 30 秒，再用新的檔名從目前時間接著錄。連續 10 次都沒錄到資料就停止。所以同一場直播可能分成多個檔案，搬到保存路徑後只會送出一次 `youtube.endstream`。

### Twitch

```sh
docker run --rm --env-file .env \
  -v "/record/output:/output" \
  -v "/record/temp:/temp_path" \
  -v "/record/twitch_unarchived:/twitch_unarchived" \
  jun112561/stream-record-tools:master \
  twitch_once USER_LOGIN -o /output -t /temp_path -u /twitch_unarchived -d
```

### CHZZK

```sh
docker run --rm --env-file .env \
  -v "/record/chzzk:/output" \
  -v "/record/temp:/temp_path" \
  jun112561/stream-record-tools:master \
  chzzk_once CHANNEL_ID "CHANNEL_ID:20260918_120000" -o /output -t /temp_path -d
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
| `ChzzkRecordPath` | CHZZK 輸出路徑 |
| `TempPath` | 暫存路徑 |
| `YouTubeUnarchivedPath` | YouTube 刪檔或私人直播保存路徑 |
| `TwitchUnarchivedPath` | Twitch 永久保存路徑 |
| `MemberOnlyPath` | YouTube 會員限定直播保存路徑 |
| `TwitchUnarchivedUserLogins` | 永久保存的 Twitch UserLogin JSON 陣列 |
| `TwitchClientId`、`TwitchClientSecret` | 查詢 Twitch 實況資訊時使用 |
| `TwitchCookieAuthToken` | Streamlink 的 Twitch Cookie Auth Token |
| `CookiesFilePath` | Netscape 格式 `cookies.txt` 的絕對路徑 |

路徑不存在、空間不足、Cookie 失效或平台限制都可能讓錄影中斷。重要直播請先用測試頻道確認設定與磁碟空間。
