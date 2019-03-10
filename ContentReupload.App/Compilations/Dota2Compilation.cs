using ContentReupload.Util;

namespace ContentReupload.App.Compilations
{
    public class Dota2Compilation : ICompilation
    {
        protected override string ChannelName => "Daily Dota";
        protected override string RedditSub => "DotA2";
        protected override string TitlePrefix => "BEST Dota Moments";
        protected override string[] DefaultTags => new string[] { "dota", "dota 2",
            "moments", "funny", "montage", "fails", "best", "compilation", "wtf", "twitch", "clips" };

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
