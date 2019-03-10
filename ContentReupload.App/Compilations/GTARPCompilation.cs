using ContentReupload.Util;

namespace ContentReupload.App.Compilations
{
    public class GTARPCompilation : ICompilation
    {
        protected override string ChannelName => "Daily RP";
        protected override string RedditSub => "RPClipsGTA";
        protected override string TitlePrefix => "BEST GTA RP Moments";
        protected override string[] DefaultTags => new string[] { "gta", "gta 5", "rp", "roleplay",
            "moments", "funny", "trolling", "fails", "best", "compilation", "twitch", "clips" };

        protected override bool UseOutro => true;

        protected override string OutroPath
        {
            get
            {
                return $"{UtilMethods.GetSolutionPath()}outros/outro.mp4";
            }
        }
    }
}
