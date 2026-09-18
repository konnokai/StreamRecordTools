# Graph Report - StreamRecordTools  (2026-09-18)

## Corpus Check
- 16 files · ~7,851 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 303 nodes · 430 edges · 12 communities
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 3 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `7033df80`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .Main
- Subscribe
- Utility
- StreamRecordTools.csproj
- Program
- StreamRecordTools
- Log
- ToolConfig
- 錄影小幫手
- .StartRecord
- Signum
- Program.cs

## God Nodes (most connected - your core abstractions)
1. `Signum` - 37 edges
2. `Subscribe` - 20 edges
3. `Utility` - 20 edges
4. `Chzzk` - 17 edges
5. `ToolConfig` - 17 edges
6. `Program` - 14 edges
7. `Log` - 8 edges
8. `錄影小幫手` - 8 edges
9. `StreamRecordTools.Command.Record` - 7 edges
10. `ChzzkRecordTests` - 7 edges

## Surprising Connections (you probably didn't know these)
- `Utility` --references--> `ToolConfig`  [EXTRACTED]
  StreamRecordTools/Utility.cs →   _Bridges community 2 → community 7_

## Import Cycles
- None detected.

## Communities (12 total, 0 thin omitted)

### Community 0 - ".Main"
Cohesion: 0.09
Nodes (17): Lazy, ResultType, TwitcastingOnceOptions, Twitcasting, ResultType, TwitchOnceOptions, Twitch, ChzzkOnceOptions (+9 more)

### Community 1 - "Subscribe"
Cohesion: 0.12
Nodes (18): DockerClient, HttpClient, ResultType, SubOptions, Task, Timer, VideoSnippet, Subscribe (+10 more)

### Community 2 - "Utility"
Cohesion: 0.07
Nodes (28): CheckResult, DateTime, IEnumerable, IList, List, ResultType, Task, YouTube (+20 more)

### Community 3 - "StreamRecordTools.csproj"
Cohesion: 0.10
Nodes (17): CommandLineParser (2.9.1), Docker.DotNet (3.125.15), Google.Apis.YouTube.v3 (1.74.0.4137), HtmlAgilityPack (1.12.4), Microsoft.NET.Test.Sdk (17.8.0), Microsoft.VisualStudio.Azure.Containers.Tools.Targets (1.23.0), Newtonsoft.Json (13.0.4), Polly (8.6.6) (+9 more)

### Community 4 - "Program"
Cohesion: 0.05
Nodes (45): Assembly, AssemblyInformationalVersionAttribute, RequiredOptions, ChzzkOnceOptions, ChannelId, StreamKey, Program, VERSION (+37 more)

### Community 5 - "StreamRecordTools"
Cohesion: 0.20
Nodes (5): StreamRecordTools, DllImport, ManagementBaseObject, Process, ProcessUtils

### Community 6 - "Log"
Cohesion: 0.18
Nodes (9): ConsoleColor, Exception, Log, LogType, Error, Info, Stream, Verb (+1 more)

### Community 7 - "ToolConfig"
Cohesion: 0.10
Nodes (18): ToolConfig, ChzzkRecordPath, CookiesFilePath, GoogleApiKey, MemberOnlyPath, RecordPath, RedisOption, TempPath (+10 more)

### Community 8 - "錄影小幫手"
Cohesion: 0.15
Nodes (12): CHZZK, Docker Compose 訂閱模式, Docker 單次錄影, Redis 錄影頻道, Twitch, YouTube, 建議使用方式, 準備 YouTube Cookie (+4 more)

### Community 9 - ".StartRecord"
Cohesion: 0.09
Nodes (21): ChzzkRecordPayload, Fact, InlineData, LockState, RecordOutcome, Regex, ChzzkOnceOptions, Process (+13 more)

### Community 10 - "Signum"
Cohesion: 0.06
Nodes (36): Signum, SIGABRT, SIGALRM, SIGBUS, SIGCHLD, SIGCLD, SIGCONT, SIGFPE (+28 more)

### Community 11 - "Program.cs"
Cohesion: 0.36
Nodes (3): StreamRecordTools.Command.Record, StreamRecordTools.Tests, StreamRecordTools.Command

## Knowledge Gaps
- **136 isolated node(s):** `Completed`, `Partial`, `Failed`, `ChannelId`, `StreamKey` (+131 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 183 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Program` connect `Program` to `.Main`, `Program.cs`?**
  _High betweenness centrality (0.242) - this node is a cross-community bridge._
- **Why does `Utility` connect `Utility` to `.Main`, `Subscribe`, `StreamRecordTools`, `ToolConfig`?**
  _High betweenness centrality (0.215) - this node is a cross-community bridge._
- **Why does `Signum` connect `Signum` to `StreamRecordTools`?**
  _High betweenness centrality (0.194) - this node is a cross-community bridge._
- **What connects `Completed`, `Partial`, `Failed` to the rest of the system?**
  _136 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.Main` be split into smaller, more focused modules?**
  _Cohesion score 0.09420289855072464 - nodes in this community are weakly interconnected._
- **Should `Subscribe` be split into smaller, more focused modules?**
  _Cohesion score 0.12477718360071301 - nodes in this community are weakly interconnected._
- **Should `Utility` be split into smaller, more focused modules?**
  _Cohesion score 0.06507936507936508 - nodes in this community are weakly interconnected._