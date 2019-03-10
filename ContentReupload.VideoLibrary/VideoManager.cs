using ContentReupload.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using WMPLib;

namespace ContentReupload.VideoLibrary
{
    public class VideoManager
    {
        public string CreateCompilationVideo(string title, List<string> clipPaths)
        {
            if (clipPaths == null || clipPaths.Count == 0)
                throw new ArgumentNullException();

            Console.WriteLine($"Creating \"{title}\" compilation video");

            string outputLocation = UtilMethods.GetDocumentsPath() + "/YouTube/Videos";
            if (!Directory.Exists(outputLocation))
            {
                Directory.CreateDirectory(outputLocation);
            }

            outputLocation += "/" + ValidateFileName(title) + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".mp4";

            string command = GetFfmpegCommand(clipPaths, outputLocation);
            ExecuteCmdCommand(command);

            Console.WriteLine($"Finished creating \"{title}\" compilation video");

            return outputLocation;
        }

        public TimeSpan GetVideoLength(string videoPath)
        {
            var player = new WindowsMediaPlayer();
            var video = player.newMedia(videoPath);
            var ret = TimeSpan.FromSeconds(video.duration);
            player.close();
            return ret;
        }

        public string ValidateFileName(string input)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9_ -]"); // only alphanumeric characters
            return rgx.Replace(input.Replace(" ", "-"), "");
        }

        // command based out of https://stackoverflow.com/a/48853654/5031684
        private string GetFfmpegCommand(List<string> clipPaths, string outputLocation)
        {
            if (clipPaths == null || clipPaths.Count == 0 || outputLocation == null)
                throw new ArgumentNullException();

            string command = $"{UtilMethods.GetSolutionPath()}ffmpeg ";
            foreach (string clip in clipPaths)
            {
                command += $"-i \"{clip}\" ";
            }
            command += "-filter_complex \"";
            for (int i = 0; i < clipPaths.Count; i++)
            {
                //command += $"[{i}:v]scale=1920:1080:force_original_aspect_ratio=1[v{i}]; ";
                command += $"[{i}:v]scale=1920:1080,setdar=16/9[v{i}]; ";
            }
            for (int i = 0; i < clipPaths.Count; i++)
            {
                command += $"[v{i}][{i}:a]";
            }
            command += $"concat=n={clipPaths.Count}:v=1:a=1[v][a]\" -map [v] -map [a] {outputLocation}";

            return command;
        }

        private void ExecuteCmdCommand(string command)
        {
            Process process = Process.Start("cmd.exe", "/c " + command);
            process.WaitForExit();
        }
    }
}
