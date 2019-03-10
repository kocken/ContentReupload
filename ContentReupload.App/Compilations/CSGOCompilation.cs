using ContentReupload.Util;

namespace ContentReupload.App.Compilations
{
    public class CSGOCompilation : ICompilation
    {
        protected override string ChannelName => "Daily CSGO";
        protected override string RedditSub => "GlobalOffensive";
        protected override string TitlePrefix => "BEST CS:GO Moments";
        protected override string[] DefaultTags => new string[] { "csgo", "cs:go", "counter strike global offensive",
            "moments", "funny", "montage", "fails", "best", "compilation", "twitch", "clips" };

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
