using ContentReupload.Common;
using ContentReupload.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ContentReupload.VideoManagement
{
    public class VideoEditor
    {
        private readonly string _clipsFolder;
        private readonly string _outputFolder;

        public VideoEditor(string clipsFolder, string outputFolder)
        {
            _clipsFolder = clipsFolder;
            _outputFolder = outputFolder;
        }

        public string CreateCompilationVideo(string title, List<VideoClip> clips)
        {
            if (clips == null || clips.Count == 0)
                throw new ArgumentNullException();

            Console.WriteLine($"Creating compilation \"{title}\"");

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            var outputLocation = _outputFolder + "/" + FileUtil.ValidateFileName(title) + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".mp4";

            var command = GetFfmpegCommand(clips, outputLocation);
            ExecuteCmdCommand(command);

            Console.WriteLine($"Finished creating \"{title}\" compilation video");

            return outputLocation;
        }

        // command based out of https://stackoverflow.com/a/48853654/5031684
        private string GetFfmpegCommand(List<VideoClip> clips, string outputLocation)
        {
            if (clips == null || clips.Count == 0 || outputLocation == null)
                throw new ArgumentNullException();

            var ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe");

            // Variables was an attempt to surpass string limitation but it failed, the limit still remains with the error "The input line is too long."
            // https://support.microsoft.com/en-us/help/830473/command-prompt-cmd-exe-command-line-string-limitation

            //var variableCommand = 
            //    "SET \"_scale=scale=1920:1080,setdar=16/9\" && " +
            //    "SET \"_font=fontfile=/Windows/Fonts/arial.ttf:fontcolor=black@0.8:box=1:boxborderw=4:boxcolor=white@0.5:x=(w-text_w)/2:y=0\" && " +
            //    $"SET \"_clips={_clipsFolder}\"";

            //var duplicates = clips
            //    .GroupBy(x => x.LocalLocation)
            //    .Where(x => x.Count() > 1)
            //    .Select(x => x.Key);

            //// Identifies clip dupes such as transitions and store them as variables
            //Dictionary<string, string> dupeDictionary = new Dictionary<string, string>();
            //for (int i = 0; duplicates.Count() > i; i++)
            //{
            //    dupeDictionary.Add($"_dupe{i}", duplicates.ElementAt(i));
            //    variableCommand += $" && SET \"_dupe{i}={duplicates.ElementAt(i)}\"";
            //}

            var command = $"CD \"{_clipsFolder}\" && {ffmpegPath} ";

            var movedClips = new List<string>();
            foreach (var clip in clips)
            {
                if (!clip.LocalLocation.StartsWith(_clipsFolder))
                {
                    var src = clip.LocalLocation;
                    var dest = _clipsFolder + "/" + src.Split('/').Last();
                    clip.LocalLocation = dest;

                    if (!movedClips.Contains(src))
                    {
                        File.Copy(src, dest, true);
                        movedClips.Add(src);
                    }
                }

                //var kvp = dupeDictionary.SingleOrDefault(x => x.Value == clip.LocalLocation);
                //if (!kvp.Equals(default(KeyValuePair<string, string>)))
                //{
                //    ffmpegCommand += $"-i \"%{kvp.Key}%\" ";
                //}
                //else if (clip.LocalLocation.StartsWith(_clipsFolder))
                //{
                //    ffmpegCommand += $"-i \"%_clips%{clip.LocalLocation.Replace(_clipsFolder, "")}\" ";
                //}
                //else
                //{
                if (clip.LocalLocation.StartsWith(_clipsFolder))
                {
                    var location = clip.LocalLocation.Replace(_clipsFolder + "/", "");
                    command += $"-i \"{location}\" ";
                }
                else
                {
                    command += $"-i \"{clip.LocalLocation}\" ";
                }
                //}
            }

            command += "-filter_complex \"";

            for (int i = 0; i < clips.Count; i++)
            {
                int fontSize = 70;
                SizeF textSize = TextRenderer.MeasureText(clips[i].Title, new Font("Arial", fontSize, FontStyle.Regular));
                while (textSize.Width >= 1920)
                {
                    fontSize--;
                    textSize = TextRenderer.MeasureText(clips[i].Title, new Font("Arial", fontSize, FontStyle.Regular));
                }

                // strip special characters as ffmpeg is buggy and doesn't handle characters like slashes and quotes
                // supposedly should be doable https://ffmpeg.org/ffmpeg-utils.html#Quoting-and-escaping 
                // but according to many it's a bug and impossible, atleast on Windows
                var rgx = new Regex("[^a-zA-Z0-9_ -!?]");
                var title = rgx.Replace(clips[i].Title, string.Empty);

                if (!clips[i].IsEngagementClip)
                {
                    command += $"[{i}:v]scale=1920:1080,setdar=16/9,drawtext=text='{title}':" +
                        $"fontsize={fontSize}:fontfile=/Windows/Fonts/arial.ttf:fontcolor=black@0.8:box=1:boxborderw=4:boxcolor=white@0.5:x=(w-text_w)/2:y=4[v{i}]; ";
                }
                else
                {
                    command += $"[{i}:v]scale=1920:1080,setdar=16/9[v{i}]; ";
                }
            }

            for (int i = 0; i < clips.Count; i++)
            {
                command += $"[v{i}][{i}:a]";
            }

            command += $"concat=n={clips.Count}:v=1:a=1[v][a]\" -map [v] -map [a] {outputLocation}";

            //return new string[] { variableCommand, ffmpegCommand };

            return command;
        }

        private void ExecuteCmdCommands(IEnumerable<string> commands)
        {
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;

            p.StartInfo = info;
            p.Start();

            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    foreach (var cmd in commands)
                    {
                        sw.WriteLine(cmd);
                    }
                }
            }
            p.WaitForExit();
        }

        private void ExecuteCmdCommand(string command)
        {
            Console.WriteLine($"Executing command:{Environment.NewLine}{command}");
            Process process = Process.Start("cmd.exe", "/c " + command);
            process.WaitForExit();
        }
    }
}
