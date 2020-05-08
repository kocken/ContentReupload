using ContentReupload.Common;

namespace ContentReupload.App.Channels
{
    public class DailyCSGOChannel : Channel
    {
        public DailyCSGOChannel(string clipsFolder, string compilationsFolder) : base(clipsFolder, compilationsFolder)
        {

        }

        protected override string ChannelName => "Daily CSGO";
        protected override string ContentFocus => "CSGO";
        protected override string RedditSub => "GlobalOffensive";
        protected override string TitlePrefix => "CSGO Highlights and Funny Moments Compilation";
        protected override string ShortTitlePrefix => "CSGO Highlights Compilation";
        protected override string[] DefaultTags => new string[] { "csgo highlights", "csgo funny moments", "csgo fails", "csgo compilation", "csgo twitch",
            "csgo", "cs go", "counter strike global offensive" };

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
