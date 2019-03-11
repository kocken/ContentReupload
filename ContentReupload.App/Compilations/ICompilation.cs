using ContentReupload.App.Db;
using ContentReupload.App.Models;
using ContentReupload.RedditLibrary;
using ContentReupload.TwitchLibrary;
using ContentReupload.Util;
using ContentReupload.VideoLibrary;
using ContentReupload.YouTubeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ContentReupload.App.Compilations
{
    public abstract class ICompilation
    {
        protected abstract string ChannelName { get; }
        protected abstract string RedditSub { get; }
        protected abstract string TitlePrefix { get; }
        protected abstract string[] DefaultTags { get; }

        protected abstract bool UseOutro { get; }
        protected abstract string OutroPath { get; }

        private readonly Database _database;
        private readonly RedditManager _redditManager;
        private readonly TwitchManager _twitchManager;
        private readonly VideoManager _videoManager;
        private readonly YouTubeManager _youtubeManager;

        public ICompilation()
        {
            _database = new Database();
            _redditManager = new RedditManager();
            _twitchManager = new TwitchManager();
            _videoManager = new VideoManager();
            _youtubeManager = new YouTubeManager();
        }

        public async Task ManageUploadsAsync()
        {
            while (true)
            {
                switch (GetCurrentCreationTask())
                {
                    case CreationTask.Yearly:
                        await CreateCompilationAsync(TimePeriod.Year);
                        break;

                    case CreationTask.Monthly:
                        await CreateCompilationAsync(TimePeriod.Month);
                        break;

                    case CreationTask.Daily:
                        await CreateCompilationAsync(TimePeriod.Day);
                        break;

                    case CreationTask.None:
                    default:
                        UtilMethods.Sleep(10 * 60 * 1000); // 10 minutes
                        break;
                }
            }
        }

        CreationTask GetCurrentCreationTask()
        {
            DateTime lastYearUpload = _database.GetLastUploadDate(ChannelName, TimePeriod.Year);
            DateTime lastMonthUpload = _database.GetLastUploadDate(ChannelName, TimePeriod.Month);
            DateTime lastDayUpload = _database.GetLastUploadDate(ChannelName, TimePeriod.Day);
            DateTime now = DateTime.UtcNow;

            if (now.Month == 1 && (lastYearUpload == default || lastYearUpload.Year != now.Year))
            {
                return CreationTask.Yearly;
            }
            else if (now.Day <= 5 && (now.Month != 1 || now.Hour >= 15) && (lastMonthUpload == default || lastMonthUpload.Month != now.Month))
            {
                return CreationTask.Monthly;
            }
            else if (lastDayUpload == default ||
                now.Subtract(lastDayUpload) >= TimeSpan.FromHours(23) && now.Hour >= 19 ||
                now.Subtract(lastDayUpload) >= TimeSpan.FromHours(26))
            {
                return CreationTask.Daily;
            }

            return CreationTask.None;
        }

        protected async Task<bool> CreateCompilationAsync(TimePeriod timePeriod)
        {
            Console.WriteLine($"Creating YouTube {timePeriod.ToString().ToLower()} compilation for the \"{ChannelName}\" channel");

            DateTime start = DateTime.UtcNow;

            List<RedditSubmission> redditSubmissions = await _redditManager.
                ObtainTopAsync(RedditSub, "clips.twitch.tv", timePeriod, timePeriod != TimePeriod.Day);

            if (redditSubmissions.Count == 0)
            {
                Console.WriteLine("No submissions found on /r/" + RedditSub);
                return false;
            }

            List<TwitchClip> twitchClips = new List<TwitchClip>();
            TimeSpan contentLength = new TimeSpan();

            for (int i = 0; i < redditSubmissions.Count && contentLength <= TimeSpan.FromSeconds(610); i++) // 10+ min vids
            {
                RedditSubmission submission = redditSubmissions.ElementAt(i);

                TwitchClip clip = _twitchManager.DownloadClip(submission.Title, submission.Url.AbsolutePath);

                if (clip != null)
                {
                    twitchClips.Add(clip);

                    contentLength = contentLength.Add(_videoManager.GetVideoLength(clip.LocalLocation));
                }
            }

            List<string> clipPaths = twitchClips.Select(x => x.LocalLocation).ToList();

            if (UseOutro)
            {
                clipPaths.Add(OutroPath);
            }

            string compilationPath = _videoManager.CreateCompilationVideo($"{ChannelName}_" +
                $"{timePeriod.ToString()}_{start.Year}-{start.Month}-{start.Day}-{start.Hour}", clipPaths);

            BuildVideoDetails(timePeriod, compilationPath, OutroPath, twitchClips, contentLength,
                out string title, out string description, out List<string> youtubeTags);

            RemoveClips(twitchClips);

            _youtubeManager.UploadVideoAsync(ChannelName, title, description, youtubeTags.ToArray(), compilationPath).Wait();

            Console.WriteLine("Deleting compilation video from local disk and storing creation-entry in DB");
            try
            {
                File.Delete(compilationPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            _database.StoreUploadEntry(ChannelName, title, timePeriod, start);

            Console.WriteLine($"Finished proccess: Created and uploaded twitch " +
                $"{timePeriod.ToString().ToLower()} compilation '{title}' at {start.ToString()}");

            return true;
        }

        protected void BuildVideoDetails(TimePeriod timePeriod, string compilationPath, string OutroPath,
            List<TwitchClip> twitchClips, TimeSpan contentLength, out string title, out string description, out List<string> youtubeTags)
        {
            Console.WriteLine("Building video details");

            youtubeTags = DefaultTags.ToList();

            title = TitlePrefix;
            switch (timePeriod)
            {
                case TimePeriod.Year:
                    title += " " + (DateTime.UtcNow.Year - 1) + " Rewind";
                    youtubeTags.Add((DateTime.UtcNow.Year - 1).ToString());
                    youtubeTags.Add("rewind");
                    break;

                case TimePeriod.Month:
                    DateTime monthDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(14));
                    title += " " + UtilMethods.GetMonth(monthDate) + " " + monthDate.Year + " Rewind";
                    youtubeTags.Add(UtilMethods.GetMonth(monthDate).ToString());
                    youtubeTags.Add(monthDate.Year.ToString());
                    youtubeTags.Add("rewind");
                    break;

                case TimePeriod.Week:
                    DateTime weekDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3));
                    title += " Week " + UtilMethods.GetWeekOfYear(weekDate) + " " + UtilMethods.GetMonth(weekDate) + " " + weekDate.Year + " Rewind";
                    youtubeTags.Add(UtilMethods.GetMonth(weekDate).ToString());
                    youtubeTags.Add(weekDate.Year.ToString());
                    youtubeTags.Add("rewind");
                    youtubeTags.Add("week");
                    break;

                case TimePeriod.Day:
                default:
                    title += " #" + (_database.GetUploadsCount(ChannelName, timePeriod) + 1);
                    break;
            }

            description = GetDescription(twitchClips, contentLength, compilationPath, title);

            youtubeTags = youtubeTags.Distinct().ToList();

            Console.WriteLine("Finished building video details");
        }

        protected string GetDescription(List<TwitchClip> twitchClips, TimeSpan contentLength, string compilationPath, string title)
        {
            TimeSpan compilationLength = _videoManager.GetVideoLength(compilationPath);
            // time difference (in seconds) between the actual content length and the invalid contentLength
            double timeDifference = compilationLength
                .Subtract(UseOutro ? _videoManager.GetVideoLength(OutroPath) : new TimeSpan())
                .Subtract(contentLength).TotalSeconds;

            double timeDifferencePerVideo = timeDifference / twitchClips.Count;

            string description = title +
                $"{Environment.NewLine}{Environment.NewLine}Twitch streamers";
            contentLength = new TimeSpan();

            double currentTimeDifference = 0;
            int secondsCompensated = 0;
            foreach (TwitchClip clip in twitchClips)
            {
                //youtubeTags.Add(clip.Channel);
                description += $"{Environment.NewLine}{contentLength.Minutes}:{contentLength.Seconds.ToString("00")} - ";
                contentLength = contentLength.Add(_videoManager.GetVideoLength(clip.LocalLocation));
                currentTimeDifference += timeDifferencePerVideo;
                int roundedDiff = Convert.ToInt32(currentTimeDifference);
                if (roundedDiff != secondsCompensated)
                {
                    contentLength = contentLength.Add(TimeSpan.FromSeconds(roundedDiff - secondsCompensated));
                    secondsCompensated = roundedDiff;
                }
                description += $"{contentLength.Minutes}:{contentLength.Seconds.ToString("00")} : " +
                    $"{clip.Channel}"; //$"https://www.twitch.tv/{clip.Channel}";
            }
            return description;
        }

        protected void RemoveClips(List<TwitchClip> twitchClips)
        {
            Console.WriteLine("Deleting used clips from local disk");

            foreach (TwitchClip clip in twitchClips)
            {
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

    enum CreationTask
    {
        Yearly, Monthly, Daily, None
    }
}
