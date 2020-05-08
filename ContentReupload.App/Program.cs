using ContentReupload.App.Channels;
using ContentReupload.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ContentReupload.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var mainFolder = FileUtil.GetDocumentsPath() + "/ContentReupload";

            var clipsFolder = mainFolder + "/Clips";
            var compilationsFolder = mainFolder + "/Compilations";

            Channel[] channels = new Channel[] {
                new TwitchGoldChannel(clipsFolder, compilationsFolder),
                new DailyCSGOChannel(clipsFolder, compilationsFolder),
                new DailyDotaChannel(clipsFolder, compilationsFolder),
                //new DailyRPChannel(clipsFolder, compilationsFolder) // not enough clips
            };

            foreach (Channel channel in channels)
            {
                Thread thread = new Thread(() => channel.RunAsync().Wait());
                thread.Start();
            }
        }
    }
}
