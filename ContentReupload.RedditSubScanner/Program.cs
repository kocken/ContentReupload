using ContentReupload.RedditLibrary;

namespace ContentReupload.RedditSubScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            RedditManager reddit = new RedditManager();

            // Top ones with a decent amount of daily hot clips as of 2019-01-27:
            // LivestreamFail 71 clips ✓ Twitch Gold
            // GlobalOffensive 51 clips ✓ Daily CSGO
            // DotA2 32 clips ✓ Daily Dota
            // RPClipsGTA 22 clips ✓ Daily RP

            var subs = reddit.GetSubsAsync("clips.twitch.tv").Result;
        }
    }
}
