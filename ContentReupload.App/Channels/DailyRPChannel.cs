using ContentReupload.Common;

namespace ContentReupload.App.Channels
{
    public class DailyRPChannel : Channel
    {
        public DailyRPChannel(string clipsFolder, string compilationsFolder) : base(clipsFolder, compilationsFolder)
        {

        }

        protected override string ChannelName => "Daily RP";
        protected override string ContentFocus => "GTA RP";
        protected override string RedditSub => "RPClipsGTA";
        protected override string TitlePrefix => "GTA RP Funny Moments Compilation";
        protected override string ShortTitlePrefix => "GTA RP Compilation";
        protected override string[] DefaultTags => new string[] { "gta rp", "gta roleplay",
            "gta funny moments", "gta trolling", "gta fails", "gta compilation" };

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
