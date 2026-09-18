# Graph Report - StreamRecordTools  (2026-08-17)

## Corpus Check
- 13 files · ~6,271 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 143 nodes · 221 edges · 12 communities (11 shown, 1 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b7d3200d`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .Info
- .SubRecord
- Utility
- StreamRecordTools.csproj
- Program
- RedisConnection
- Log
- .StartRecord
- 錄影小幫手
- UptimeKumaClient
- Utility.cs
- Program.cs

## God Nodes (most connected - your core abstractions)
1. `Utility` - 14 edges
2. `Program` - 12 edges
3. `Subscribe` - 10 edges
4. `Log` - 9 edges
5. `UptimeKumaClient` - 7 edges
6. `Twitch` - 6 edges
7. `YouTube` - 6 edges
8. `Twitcasting` - 5 edges
9. `RedisConnection` - 5 edges
10. `錄影小幫手` - 5 edges

## Surprising Connections (you probably didn't know these)
- `Utility` --references--> `ToolConfig`  [EXTRACTED]
  StreamRecordTools/Utility.cs → StreamRecordTools/ToolConfig.cs

## Import Cycles
- None detected.

## Communities (12 total, 1 thin omitted)

### Community 0 - ".Info"
Cohesion: 0.15
Nodes (11): bool, ResultType, string, Twitcasting, bool, ResultType, string, Twitch (+3 more)

### Community 1 - ".SubRecord"
Cohesion: 0.28
Nodes (7): DockerClient, ResultType, Task, Timer, VideoSnippet, Subscribe, SubOptions

### Community 2 - "Utility"
Cohesion: 0.15
Nodes (11): CheckResult, IEnumerable, IList, ConnectionMultiplexer, Task, VideoSnippet, CheckResult, Utility (+3 more)

### Community 3 - "StreamRecordTools.csproj"
Cohesion: 0.13
Nodes (12): net10.0, CommandLineParser (2.9.1), Docker.DotNet (3.125.15), Google.Apis.YouTube.v3 (1.74.0.4137), HtmlAgilityPack (1.12.4), Microsoft.VisualStudio.Azure.Containers.Tools.Targets (1.23.0), Newtonsoft.Json (13.0.4), Polly (8.6.6) (+4 more)

### Community 4 - "Program"
Cohesion: 0.18
Nodes (13): Assembly, RequiredOptions, Program, RequiredOptions, ResultType, Status, SubOptions, TwitcastingOnceOptions (+5 more)

### Community 5 - "RedisConnection"
Cohesion: 0.17
Nodes (7): StreamRecordTools, Lazy, ConnectionMultiplexer, string, RedisConnection, ToolConfig, Type

### Community 6 - "Log"
Cohesion: 0.27
Nodes (5): ConsoleColor, Exception, object, Log, LogType

### Community 7 - ".StartRecord"
Cohesion: 0.22
Nodes (6): DateTime, bool, ResultType, string, Task, YouTube

### Community 8 - "錄影小幫手"
Cohesion: 0.20
Nodes (9): Docker 環境，Sub 模式, Docker 環境，單一直播錄影模式, Redis 頻道, Twitch, YouTube, 環境變數說明, 直接執行程式, 製作 `cookies.txt` (+1 more)

### Community 9 - "UptimeKumaClient"
Cohesion: 0.25
Nodes (6): HttpClient, bool, string, Task, Timer, UptimeKumaClient

### Community 10 - "Utility.cs"
Cohesion: 0.32
Nodes (4): DllImport, Process, ProcessUtils, Signum

## Knowledge Gaps
- **24 isolated node(s):** `LogType`, `Status`, `ResultType`, `RequiredOptions`, `YTOnceOnDockerOptions` (+19 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Program` connect `Program` to `.Info`, `Program.cs`?**
  _High betweenness centrality (0.149) - this node is a cross-community bridge._
- **Why does `Utility` connect `Utility` to `.Info`, `.SubRecord`, `RedisConnection`, `.StartRecord`, `Utility.cs`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **What connects `LogType`, `Status`, `ResultType` to the rest of the system?**
  _24 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `StreamRecordTools.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.13333333333333333 - nodes in this community are weakly interconnected._