using StreamRecordTools.Command.Record;

namespace StreamRecordTools.Tests
{
    /// <summary>
    /// CHZZK 錄影的最小可執行檢查：涵蓋 payload 解析、Streamlink 參數、唯一檔名與失敗收尾判斷。
    /// Redis 互斥屬跨程序行為，需以隔離 Redis 實測，不在此涵蓋。
    /// </summary>
    public class ChzzkRecordTests
    {
        private const string ChannelId = "4de764d9dad3b25602284be6db3ac647";
        private const string StreamKey = ChannelId + ":20260918_120000";

        [Fact]
        public void TryParseRequestAcceptsSharedContractPayload()
        {
            string payload = $"{{\"channelId\":\"{ChannelId}\",\"streamKey\":\"{StreamKey}\"}}";

            Assert.True(Chzzk.TryParseRequest(payload, out string channelId, out string streamKey));
            Assert.Equal(ChannelId, channelId);
            Assert.Equal(StreamKey, streamKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{}")]
        [InlineData("not json")]
        [InlineData("{\"channelId\":\"4de764d9dad3b25602284be6db3ac647\"}")]
        [InlineData("{\"channelId\":\"https://chzzk.naver.com/4de764d9dad3b25602284be6db3ac647\",\"streamKey\":\"x\"}")]
        [InlineData("{\"channelId\":\"4de764d9dad3b25602284be6db3ac647\",\"streamKey\":\"4de764d9dad3b25602284be6db3ac648:20260918_120000\"}")]
        [InlineData("{\"channelId\":\"4de764d9dad3b25602284be6db3ac647\",\"streamKey\":\"4de764d9dad3b25602284be6db3ac647:2026-09-18 12:00:00\"}")]
        public void TryParseRequestRejectsInvalidPayload(string payload)
        {
            Assert.False(Chzzk.TryParseRequest(payload, out _, out _));
        }

        [Fact]
        public void StreamKeyTimestampHasNoColonForWindowsFileName()
        {
            Assert.Equal("20260918_120000", Chzzk.GetStreamKeyTimestamp(StreamKey));
        }

        [Fact]
        public void BuildStreamlinkArgumentsUseBestAndControlledOutput()
        {
            string[] arguments = Chzzk.BuildStreamlinkArguments(@"C:\temp\chzzk_test.ts", ChannelId);

            Assert.Equal(["--progress", "no", "--output", @"C:\temp\chzzk_test.ts",
                $"https://chzzk.naver.com/live/{ChannelId}", "best"], arguments);
        }

        [Fact]
        public void CreateUniqueFilePathDoesNotOverwriteExistingRecording()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"chzzk_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            try
            {
                string first = Chzzk.CreateUniqueFilePath(directory, "chzzk_base", ".ts");
                Assert.Equal(Path.Combine(directory, "chzzk_base.ts"), first);

                File.WriteAllText(first, "partial");
                string second = Chzzk.CreateUniqueFilePath(directory, "chzzk_base", ".ts");
                Assert.Equal(Path.Combine(directory, "chzzk_base_2.ts"), second);
                Assert.Equal("partial", File.ReadAllText(first));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Theory]
        [InlineData(0, true, 1024L, Chzzk.RecordOutcome.Completed)]
        [InlineData(1, true, 1024L, Chzzk.RecordOutcome.Partial)]
        [InlineData(0, true, 0L, Chzzk.RecordOutcome.Failed)]
        [InlineData(0, false, 0L, Chzzk.RecordOutcome.Failed)]
        [InlineData(1, false, 0L, Chzzk.RecordOutcome.Failed)]
        public void DecideOutcomeDoesNotTreatEveryExitAsSuccess(int exitCode, bool fileExists, long fileLength,
            Chzzk.RecordOutcome expected)
        {
            Assert.Equal(expected, Chzzk.DecideOutcome(exitCode, fileExists, fileLength));
        }
    }
}
