# Graph Report - StreamRecordTools  (2026-09-15)

## Corpus Check
- 13 files · ~6,271 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 244 nodes · 330 edges · 13 communities
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 6 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `77645743`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .StartRecord
- .SubRecord
- Utility
- StreamRecordTools.csproj
- Program
- RedisConnection
- Log
- ToolConfig
- 錄影小幫手
- .Init
- Signum
- .StartRecord
- CheckResult

## God Nodes (most connected - your core abstractions)
1. `Signum` - 37 edges
2. `Utility` - 20 edges
3. `Subscribe` - 17 edges
4. `ToolConfig` - 16 edges
5. `Program` - 13 edges
6. `Log` - 8 edges
7. `LogType` - 6 edges
8. `Status` - 6 edges
9. `CheckResult` - 6 edges
10. `ResultType` - 5 edges

## Surprising Connections (you probably didn't know these)
- `Utility` --references--> `ToolConfig`  [EXTRACTED]
  StreamRecordTools/Utility.cs →   _Bridges community 2 → community 7_

## Import Cycles
- None detected.

## Communities (13 total, 0 thin omitted)

### Community 0 - ".StartRecord"
Cohesion: 0.17
Nodes (9): ResultType, TwitchOnceOptions, Twitch, SubOptions, TwitcastingOnceOptions, TwitchOnceOptions, TwitchAPI, YTOnceOnDockerOptions (+1 more)

### Community 1 - ".SubRecord"
Cohesion: 0.17
Nodes (14): DockerClient, ResultType, SubOptions, Task, Timer, VideoSnippet, Subscribe, IsDisableLiveFromStart (+6 more)

### Community 2 - "Utility"
Cohesion: 0.08
Nodes (24): CheckResult, DateTime, IEnumerable, IList, List, ManagementBaseObject, Process, ResultType (+16 more)

### Community 3 - "StreamRecordTools.csproj"
Cohesion: 0.13
Nodes (12): net10.0, CommandLineParser (2.9.1), Docker.DotNet (3.125.15), Google.Apis.YouTube.v3 (1.74.0.4137), HtmlAgilityPack (1.12.4), Microsoft.VisualStudio.Azure.Containers.Tools.Targets (1.23.0), Newtonsoft.Json (13.0.4), Polly (8.6.6) (+4 more)

### Community 4 - "Program"
Cohesion: 0.05
Nodes (42): Assembly, AssemblyInformationalVersionAttribute, RequiredOptions, Program, VERSION, RequiredOptions, DisableRedis, OutputPath (+34 more)

### Community 5 - "RedisConnection"
Cohesion: 0.17
Nodes (7): StreamRecordTools, DllImport, Lazy, ConnectionMultiplexer, RedisConnection, Instance, ProcessUtils

### Community 6 - "Log"
Cohesion: 0.18
Nodes (9): ConsoleColor, Exception, Log, LogType, Error, Info, Stream, Verb (+1 more)

### Community 7 - "ToolConfig"
Cohesion: 0.11
Nodes (17): ToolConfig, CookiesFilePath, GoogleApiKey, MemberOnlyPath, RecordPath, RedisOption, TempPath, TwitcastingRecordPath (+9 more)

### Community 8 - "錄影小幫手"
Cohesion: 0.20
Nodes (9): Docker 環境，Sub 模式, Docker 環境，單一直播錄影模式, Redis 頻道, Twitch, YouTube, 環境變數說明, 直接執行程式, 製作 `cookies.txt` (+1 more)

### Community 9 - ".Init"
Cohesion: 0.43
Nodes (4): HttpClient, Task, Timer, UptimeKumaClient

### Community 10 - "Signum"
Cohesion: 0.06
Nodes (36): Signum, SIGABRT, SIGALRM, SIGBUS, SIGCHLD, SIGCLD, SIGCONT, SIGFPE (+28 more)

### Community 11 - ".StartRecord"
Cohesion: 0.23
Nodes (5): StreamRecordTools.Command.Record, StreamRecordTools.Command, ResultType, TwitcastingOnceOptions, Twitcasting

### Community 12 - "CheckResult"
Cohesion: 0.33
Nodes (6): CheckResult, CookieFileError, CookieFileNotFound, Ok, OtherError, Redirect

## Knowledge Gaps
- **119 isolated node(s):** `NetworkId`, `OutputPath`, `TempPath`, `YouTubeUnarchivedOutputPath`, `TwitchUnarchivedOutputPath` (+114 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 158 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Program` connect `Program` to `.StartRecord`, `.StartRecord`?**
  _High betweenness centrality (0.281) - this node is a cross-community bridge._
- **Why does `Signum` connect `Signum` to `RedisConnection`?**
  _High betweenness centrality (0.239) - this node is a cross-community bridge._
- **Why does `Utility` connect `Utility` to `.StartRecord`, `.SubRecord`, `RedisConnection`, `ToolConfig`, `CheckResult`?**
  _High betweenness centrality (0.176) - this node is a cross-community bridge._
- **What connects `NetworkId`, `OutputPath`, `TempPath` to the rest of the system?**
  _119 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Utility` be split into smaller, more focused modules?**
  _Cohesion score 0.07661290322580645 - nodes in this community are weakly interconnected._
- **Should `StreamRecordTools.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.13333333333333333 - nodes in this community are weakly interconnected._
- **Should `Program` be split into smaller, more focused modules?**
  _Cohesion score 0.04983388704318937 - nodes in this community are weakly interconnected._