namespace StreamRecordTools.Tests
{
    public class YouTubeVideoPageTests
    {
        private static string Page(string playabilityStatus)
            => $"<script>var ytInitialPlayerResponse = {{\"playabilityStatus\":{playabilityStatus},\"videoDetails\":{{}}}};var meta = 1;</script>";

        [Theory]
        [InlineData("{\"status\":\"UNPLAYABLE\",\"reason\":\"members\",\"errorScreen\":{\"playerLegacyDesktopYpcOfferRenderer\":{\"offerId\":\"sponsors_only_video\"}}}",
            YouTubeVideoPageStatus.MembersOnly)]
        [InlineData("{\"status\":\"LOGIN_REQUIRED\",\"reason\":\"Private video\"}", YouTubeVideoPageStatus.Private)]
        [InlineData("{\"status\":\"LOGIN_REQUIRED\",\"reason\":\"Sign in to confirm your age\"}", YouTubeVideoPageStatus.AgeRestricted)]
        [InlineData("{\"status\":\"AGE_VERIFICATION_REQUIRED\"}", YouTubeVideoPageStatus.AgeRestricted)]
        [InlineData("{\"status\":\"LOGIN_REQUIRED\",\"reason\":\"Sign in to confirm you're not a bot\"}", YouTubeVideoPageStatus.Unknown)]
        [InlineData("{\"status\":\"ERROR\",\"reason\":\"This video is unavailable\"}", YouTubeVideoPageStatus.Unavailable)]
        [InlineData("{\"status\":\"ERROR\",\"reason\":\"This video may be inappropriate for some users\"}", YouTubeVideoPageStatus.Unavailable)]
        [InlineData("{\"status\":\"OK\"}", YouTubeVideoPageStatus.Public)]
        [InlineData("{\"status\":\"LIVE_STREAM_OFFLINE\"}", YouTubeVideoPageStatus.Unknown)]
        public void ParseReadsPlayabilityStatus(string playabilityStatus, YouTubeVideoPageStatus expected)
            => Assert.Equal(expected, YouTubeVideoPage.Parse(Page(playabilityStatus)));

        [Theory]
        [InlineData(null)]
        [InlineData("<html></html>")]
        [InlineData("var ytInitialPlayerResponse = {\"playabilityStatus\":")]
        public void ParseReturnsUnknownForUnexpectedPage(string html)
            => Assert.Equal(YouTubeVideoPageStatus.Unknown, YouTubeVideoPage.Parse(html));
    }
}
