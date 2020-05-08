using ContentReupload.Reddit;

namespace ContentReupload.Reddit.Scanner
{
    class Program
    {
        static void Main(string[] args)
        {
            RedditDownloader reddit = new RedditDownloader();

            string twitchClipDomain = "clips.twitch.tv";

            // Top ones with a decent amount of daily hot clips as of 2019-01-27:
            // LivestreamFail 71 clips ✓ Twitch Gold
            // GlobalOffensive 51 clips ✓ Daily CSGO
            // DotA2 32 clips ✓ Daily Dota
            // RPClipsGTA 22 clips ✓ Daily RP

            var subs = reddit.FindSubsAsync(twitchClipDomain).Result;
        }
    }
}
