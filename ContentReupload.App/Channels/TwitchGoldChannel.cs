using ContentReupload.Common;

namespace ContentReupload.App.Channels
{
    public class TwitchGoldChannel : Channel
    {
        public TwitchGoldChannel(string clipsFolder, string compilationsFolder) : base(clipsFolder, compilationsFolder)
        {

        }

        protected override string ChannelName => "Twitch Gold";
        protected override string ContentFocus => "Twitch";
        protected override string RedditSub => "LivestreamFail";
        protected override string TitlePrefix => "Twitch Fails and Funny Moments Compilation";
        protected override string ShortTitlePrefix => "Twitch Fails Compilation";
        protected override string[] DefaultTags => new string[] { "twitch funny moments", "twitch fails", "twitchfails", "twitch epic moments", "twitch moments", "twitch livestream fails",
            "livestreamfails", "stream fails", "livestream moments", "twitch compilation", "twitch highlights", "twitch clips", "twitch", "ultimate twitch fails compilation" };

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
