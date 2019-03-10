using ContentReupload.Util;

namespace ContentReupload.App.Compilations
{
    public class GeneralTwitchCompilation : ICompilation
    {
        protected override string ChannelName => "Twitch Gold";
        protected override string RedditSub => "LivestreamFail";
        protected override string TitlePrefix => "BEST Twitch Fails And Moments";
        protected override string[] DefaultTags => new string[] { "twitch", "fails", "moments", "clips", "compilation", "best" };

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
