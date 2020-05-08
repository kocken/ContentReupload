using Microsoft.WindowsAPICodePack.Shell;
using System;

namespace ContentReupload.VideoManagement
{
    public class VideoUtil
    {
        public static TimeSpan GetVideoLength(string videoPath)
        {
            // WindowsMediaPlayer does not account for milliseconds, only seconds and higher
            //var player = new WindowsMediaPlayer();
            //var video = player.newMedia(videoPath);
            //var ret = TimeSpan.FromSeconds(video.duration);
            //player.close();
            //return ret;

            ShellFile sf = ShellFile.FromFilePath(videoPath);
            long.TryParse(sf.Properties.System.Media.Duration.Value.ToString(), out long nanoSeconds);

            return new TimeSpan(nanoSeconds);
        }
    }
}
