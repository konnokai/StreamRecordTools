using StreamRecordTools.Command;
using StreamRecordTools.Command.Record;

namespace StreamRecordTools.Tests
{
    public class YouTubeRecordTests
    {
        private const string VideoId = "dQw4w9WgXcQ";
        private const string Output = "/temp_path/youtube_UC_20261015_120000_dQw4w9WgXcQ.%(ext)s";

        [Fact]
        public void BuildYtDlpArgumentsInDockerUseCookieFileWithoutFormatSelector()
        {
            string[] arguments = YouTube.BuildYtDlpArguments(VideoId, Output, isDisableLiveFromStart: true, inDocker: true, browser: "firefox");

            Assert.Equal([$"https://www.youtube.com/watch?v={VideoId}", "-o", Output, "--wait-for-video", "15",
                "--cookies", "/app/cookies.txt"], arguments);
        }

        [Fact]
        public void BuildYtDlpArgumentsOutsideDockerUseBrowserCookiesAndLiveFromStart()
        {
            string[] arguments = YouTube.BuildYtDlpArguments(VideoId, Output, isDisableLiveFromStart: false, inDocker: false, browser: "chrome");

            Assert.Equal([$"https://www.youtube.com/watch?v={VideoId}", "-o", Output, "--wait-for-video", "15",
                "--live-from-start", "--cookies-from-browser", "chrome"], arguments);
            Assert.DoesNotContain("-f", arguments);
            Assert.DoesNotContain("--mark-watched", arguments);
        }

        [Theory]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ.mp4", true)]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ.webm", true)]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ.mkv", true)]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ.mp4.part", false)]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ.f299.mp4", false)]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ.temp.mp4", false)]
        [InlineData("youtube_UC_20261015_120000_dQw4w9WgXcQ_err.txt", false)]
        public void IsRecordedVideoFileOnlyAcceptsFinalOutput(string fileName, bool expected)
        {
            Assert.Equal(expected, YouTube.IsRecordedVideoFile(Path.Combine("temp", fileName), VideoId));
        }

        [Theory]
        [InlineData(true, false, new[] { "yt_once_on_docker", VideoId, "--disable-live-from-start" })]
        [InlineData(false, true, new[] { "yt_once_on_docker", VideoId, "--dont-send-start-message" })]
        [InlineData(false, false, new[] { "yt_once_on_docker", VideoId })]
        [InlineData(true, true, new[] { "yt_once_on_docker", VideoId, "--disable-live-from-start", "--dont-send-start-message" })]
        public void BuildYouTubeContainerCommandOnlyAddsEnabledFlags(bool isDisableLiveFromStart, bool dontSendStartMessage, string[] expected)
        {
            Assert.Equal(expected, Subscribe.BuildYouTubeContainerCommand(VideoId, isDisableLiveFromStart, dontSendStartMessage));
        }
    }
}
