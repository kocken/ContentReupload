using ContentReupload.App.Db;
using ContentReupload.Common;
using ContentReupload.Common.Models;
using ContentReupload.Reddit;
using ContentReupload.Reddit.Models;
using ContentReupload.Twitch;
using ContentReupload.VideoManagement;
using ContentReupload.YouTube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContentReupload.App.Channels
{
    public abstract class Channel
    {
        protected abstract string ChannelName { get; }
        protected abstract string ContentFocus { get; }
        protected abstract string RedditSub { get; }

        protected abstract string TitlePrefix { get; }
        protected abstract string ShortTitlePrefix { get; }
        protected abstract string[] DefaultTags { get; }

        protected abstract bool UseTransition { get; }
        protected abstract string TransitionPath { get; }

        protected abstract bool UseOutro { get; }
        protected abstract string OutroPath { get; }
        protected abstract string OutroSong { get; }

        private readonly DatabaseUtil _databaseUtil;

        private readonly RedditDownloader _redditDownloader;
        private readonly TwitchDownloader _twitchDownloader;

        private readonly VideoEditor _videoEditor;

        private readonly YouTubeUploader _youtubeUploader;

        private readonly string[] _excludedChannels = new string[] { "alsojakob", "macaiyla", "alinity" }; // people who have/might copystrike

        public Channel(string clipsFolder, string compilationsFolder)
        {
            _databaseUtil = new DatabaseUtil();
            _redditDownloader = new RedditDownloader();
            _twitchDownloader = new TwitchDownloader(clipsFolder);
            _videoEditor = new VideoEditor(clipsFolder, compilationsFolder);
            _youtubeUploader = new YouTubeUploader();
        }

        public async Task RunAsync()
        {
            while (true)
            {
                try
                {
                    switch (GetJob())
                    {
                        case JobAction.Yearly_Compilation:
                            await CreateCompilationAsync(TimePeriod.Year);
                            break;

                        case JobAction.Monthly_Compilation:
                            await CreateCompilationAsync(TimePeriod.Month);
                            break;

                        case JobAction.Daily_Compilation:
                            await CreateCompilationAsync(TimePeriod.Day);
                            break;

                        case JobAction.Wait:
                        default:
                            Thread.Sleep(60000); // 1 minute
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        JobAction GetJob()
        {
            DateTime now = DateTime.UtcNow;

            DateTime lastYearUpload = _databaseUtil.GetLastUploadDate(ChannelName, TimePeriod.Year);
            if (now.Month == 1 && (lastYearUpload == default || lastYearUpload.Year != now.Year))
            {
                return JobAction.Yearly_Compilation;
            }

            DateTime lastMonthUpload = _databaseUtil.GetLastUploadDate(ChannelName, TimePeriod.Month);
            if (now.Day <= 5 && (now.Month != 1 || now.Hour >= 15) && (lastMonthUpload == default || lastMonthUpload.Month != now.Month))
            {
                return JobAction.Monthly_Compilation;
            }

            DateTime lastDayUpload = _databaseUtil.GetLastUploadDate(ChannelName, TimePeriod.Day);
            if (lastDayUpload == default ||
                now.Subtract(lastDayUpload) >= TimeSpan.FromHours(23) && now.Hour >= 17 ||
                now.Subtract(lastDayUpload) >= TimeSpan.FromHours(26))
            {
                return JobAction.Daily_Compilation;
            }

            return JobAction.Wait;
        }

        protected async Task<bool> CreateCompilationAsync(TimePeriod timePeriod)
        {
            Console.WriteLine($"Creating YouTube {timePeriod.ToString().ToLower()} compilation for the \"{ChannelName}\" channel");

            DateTime time = DateTime.UtcNow;

            List<RedditSubmission> redditSubmissions = await _redditDownloader.ObtainTopAsync(RedditSub, _twitchDownloader.ClipDomain, timePeriod, true);

            if (redditSubmissions.Count == 0)
            {
                Console.WriteLine("No submissions found on /r/" + RedditSub);
                return false;
            }

            List<VideoClip> videoClips = new List<VideoClip>();
            TimeSpan compilationLength = new TimeSpan();

            VideoClip transitionClip = null;
            if (UseTransition)
            {
                var transitionLength = VideoUtil.GetVideoLength(TransitionPath);

                transitionClip = new VideoClip
                {
                    Channel = ChannelName,
                    Title = "transition",
                    Length = transitionLength,
                    LocalLocation = TransitionPath,
                    IsEngagementClip = true
                };
            }

            for (int i = 0; i < redditSubmissions.Count; i++)
            {
                if (compilationLength >= TimeSpan.FromMinutes(10)) // stop adding clips when 10+ min long
                    break;

                RedditSubmission submission = redditSubmissions.ElementAt(i);

                VideoClip clip = _twitchDownloader.DownloadClip(submission.Title, submission.Url.AbsolutePath);

                if (clip != null)
                {
                    if (_excludedChannels.Contains(clip.Channel.ToLower()))
                    {
                        try
                        {
                            File.Delete(clip.LocalLocation);
                        }
                        catch (Exception) { }
                        continue;
                    }

                    var clipLength = VideoUtil.GetVideoLength(clip.LocalLocation);
                    clip.Length = clipLength;

                    compilationLength = compilationLength.Add(clipLength);

                    videoClips.Add(clip);

                    if (UseTransition)
                    {
                        compilationLength = compilationLength.Add(transitionClip.Length);

                        videoClips.Add(transitionClip);
                    }
                }
            }

            if (videoClips.Count == 0)
            {
                Console.WriteLine("No clips were downloaded");
                _databaseUtil.StoreUploadEntry(ChannelName, "ERROR_NO_CLIPS", timePeriod, time);
                return false;
            }

            if (UseOutro)
            {
                var outroLength = VideoUtil.GetVideoLength(OutroPath);
                compilationLength = compilationLength.Add(outroLength);

                videoClips.Add(new VideoClip {
                    Channel = ChannelName,
                    Title = "outro",
                    Length = outroLength,
                    LocalLocation = OutroPath,
                    IsEngagementClip = true
                });
            }

            string compilationPath = _videoEditor.CreateCompilationVideo($"{ChannelName}_" +
                $"{timePeriod.ToString()}_{time.Year}-{time.Month}-{time.Day}-{time.Hour}", videoClips);

            if (!File.Exists(compilationPath))
            {
                Console.WriteLine($"Failed to create compilation for channel '{ChannelName}'");
                _databaseUtil.StoreUploadEntry(ChannelName, "COMPILATION_CREATION_ERROR", timePeriod, time);

                return false;
            }

            BuildVideoDetails(timePeriod, compilationPath, videoClips, 
                out string title, out string description, out List<string> youtubeTags);

            await _youtubeUploader.UploadVideoAsync(ChannelName, title, description, youtubeTags.ToArray(), compilationPath);

            RemoveClips(videoClips);

            Console.WriteLine("Deleting compilation video from local disk and storing creation-entry in DB");
            try
            {
                File.Delete(compilationPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _databaseUtil.StoreUploadEntry(ChannelName, title, timePeriod, time);

            Console.WriteLine($"Finished proccess: Created and uploaded video " +
                $"{timePeriod.ToString().ToLower()} compilation '{title}' at {time.ToString()}");

            return true;
        }

        protected void BuildVideoDetails(TimePeriod timePeriod, string compilationPath,
            List<VideoClip> videoClips, out string title, out string description, out List<string> youtubeTags)
        {
            Console.WriteLine("Building video details");

            youtubeTags = DefaultTags.ToList();

            title = TitlePrefix;
            switch (timePeriod)
            {
                case TimePeriod.Year:
                    title += " " + (DateTime.UtcNow.Year - 1) + " Rewind";
                    youtubeTags.Insert(0, $"{ContentFocus.ToLower()} {(DateTime.UtcNow.Year - 1).ToString()}");
                    youtubeTags.Insert(1, $"{ContentFocus.ToLower()} rewind");
                    break;

                case TimePeriod.Month:
                    DateTime monthDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(14));
                    title += " " + DateUtil.GetMonth(monthDate) + " " + monthDate.Year + " Rewind";
                    youtubeTags.Insert(0, $"{ContentFocus.ToLower()} {monthDate.Year}");
                    youtubeTags.Insert(1, $"{ContentFocus.ToLower()} rewind");
                    break;

                case TimePeriod.Week:
                    DateTime weekDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3));
                    title += " Week " + DateUtil.GetWeekOfYear(weekDate) + " " + DateUtil.GetMonth(weekDate) + " " + weekDate.Year + " Rewind";
                    youtubeTags.Insert(0, $"{ContentFocus.ToLower()} {weekDate.Year}");
                    youtubeTags.Insert(1, $"{ContentFocus.ToLower()} rewind");
                    break;

                case TimePeriod.Day:
                default:
                    title += " #" + (_databaseUtil.GetUploadsCount(ChannelName, timePeriod) + 1);
                    break;
            }

            description = GetDescription(videoClips, title);

            youtubeTags = youtubeTags.Distinct().ToList();

            Console.WriteLine("Finished building video details");
        }

        protected string GetDescription(List<VideoClip> videoClips, string title)
        {
            TimeSpan length = new TimeSpan();

            string description = title +
                $"{Environment.NewLine}{Environment.NewLine}Twitch streamers";

            foreach (VideoClip clip in videoClips)
            {
                if (clip.IsEngagementClip)
                {
                    length = length.Add(clip.Length);
                    continue;
                }

                description += $"{Environment.NewLine}{length.Minutes}:{length.Seconds.ToString("00")} - ";
                length = length.Add(clip.Length);
                description += $"{length.Minutes}:{length.Seconds.ToString("00")} : " +
                    $"{clip.Channel}"; //$"https://www.twitch.tv/{clip.Channel}";
            }

            if (UseOutro && OutroSong != null)
                description += $"{Environment.NewLine}{Environment.NewLine}Outro song: {OutroSong}";

            return description;
        }

        protected void RemoveClips(List<VideoClip> videoClips)
        {
            Console.WriteLine("Deleting used clips from local disk");

            foreach (VideoClip clip in videoClips)
            {
                if (clip.IsEngagementClip)
                    continue;

                try
                {
                    File.Delete(clip.LocalLocation);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("Finished deleting used clips from local disk");
        }
    }

    enum JobAction
    {
        Yearly_Compilation, Monthly_Compilation, Daily_Compilation, Wait
    }
}
