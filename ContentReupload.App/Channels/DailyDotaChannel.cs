using ContentReupload.Common;

namespace ContentReupload.App.Channels
{
    public class DailyDotaChannel : Channel
    {
        public DailyDotaChannel(string clipsFolder, string compilationsFolder) : base(clipsFolder, compilationsFolder)
        {

        }

        protected override string ChannelName => "Daily Dota";
        protected override string ContentFocus => "Dota 2";
        protected override string RedditSub => "DotA2";
        protected override string TitlePrefix => "Dota 2 Fails and Funny Moments Compilation";
        protected override string ShortTitlePrefix => "Dota 2 Fails Compilation";
        protected override string[] DefaultTags => new string[] { "dota 2 fails", "dota 2 funny moments",
            "dota 2 wtf moments", "dota 2 compilation", "dota 2 twitch", "dota 2", "dota" };

        protected override bool UseTransition => true;

        protected override string TransitionPath
        {
            get
            {
                return FileUtil.GetSolutionPath() + "/Engagement/Transitions/Toadfilms.mp4";
            }
        }

        protected override bool UseOutro => true;

        protected override string OutroPath
        {
            get
            {
                return FileUtil.GetSolutionPath() + "/Engagement/Outros/Outro.mp4";
            }
        }

        protected override string OutroSong => "Sappheiros - Dawn";
    }
}
