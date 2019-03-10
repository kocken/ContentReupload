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


        public async Task ManageUploadsAsync()
        {
            while (true)
            {
                DateTime lastYearUpload = GetLastUploadDate(TimePeriod.Year);
                DateTime lastMonthUpload = GetLastUploadDate(TimePeriod.Month);
                DateTime lastDayUpload = GetLastUploadDate(TimePeriod.Day);
                DateTime now = DateTime.UtcNow;

                if (now.Month == 1 && (lastYearUpload == default || lastYearUpload.Year != now.Year))
                {
                    await CreateCompilationAsync(TimePeriod.Year);
                }
                else if (now.Day <= 5 && (now.Month != 1 || now.Hour >= 15) && (lastMonthUpload == default || lastMonthUpload.Month != now.Month))
                {
                    await CreateCompilationAsync(TimePeriod.Month);
                }
                else if (lastDayUpload == default ||
                    now.Subtract(lastDayUpload) >= TimeSpan.FromHours(23) && now.Hour >= 19 ||
                    now.Subtract(lastDayUpload) >= TimeSpan.FromHours(26))
                {
                    await CreateCompilationAsync(TimePeriod.Day);
                }
                else
                {
                    UtilMethods.Sleep(10 * 60 * 1000); // 10 minutes
                }
            }
        }

        protected async Task<bool> CreateCompilationAsync(TimePeriod timePeriod)
        {
            Console.WriteLine($"Creating YouTube {timePeriod.ToString().ToLower()} compilation for the \"{ChannelName}\" channel");

            DateTime start = DateTime.UtcNow;

            List<RedditSubmission> redditSubmissions = await new RedditManager().
                ObtainTopAsync(RedditSub, "clips.twitch.tv", timePeriod, timePeriod != TimePeriod.Day);

            if (redditSubmissions.Count == 0)
            {
                Console.WriteLine("No submissions found on /r/" + RedditSub);
                return false;
            }

            TwitchManager twitchManager = new TwitchManager();
            VideoManager videoManager = new VideoManager();

            List<TwitchClip> twitchClips = new List<TwitchClip>();
            TimeSpan contentLength = new TimeSpan();

            for (int i = 0; i < redditSubmissions.Count && contentLength <= TimeSpan.FromSeconds(610); i++) // 10+ min vids
            {
                RedditSubmission submission = redditSubmissions.ElementAt(i);

                TwitchClip clip = twitchManager.DownloadClip(submission.Title, submission.Url.AbsolutePath);

                if (clip != null)
                {
                    twitchClips.Add(clip);

                    contentLength = contentLength.Add(videoManager.GetVideoLength(clip.LocalLocation));
                }
            }

            List<string> clipPaths = twitchClips.Select(x => x.LocalLocation).ToList();

            if (UseOutro)
            {
                clipPaths.Add(OutroPath);
            }

            string compilationPath = videoManager.CreateCompilationVideo($"{ChannelName}_" +
                $"{timePeriod.ToString()}_{start.Year}-{start.Month}-{start.Day}-{start.Hour}", clipPaths);

            BuildVideoDetails(timePeriod, compilationPath, OutroPath, twitchClips, contentLength,
                out string title, out string description, out List<string> youtubeTags);

            RemoveClips(twitchClips);

            new YouTubeManager().UploadVideoAsync(ChannelName, title, description, youtubeTags.ToArray(), compilationPath).Wait();

            Console.WriteLine("Deleting compilation video from local disk and storing creation-entry in DB");
            try
            {
                File.Delete(compilationPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            StoreUploadEntryToDb(title, timePeriod, start);

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
                    title += " #" + (GetUploadsCount(timePeriod) + 1);
                    break;
            }

            description = GetDescription(twitchClips, contentLength, compilationPath, title);

            youtubeTags = youtubeTags.Distinct().ToList();

            Console.WriteLine("Finished building video details");
        }

        protected string GetDescription(List<TwitchClip> twitchClips, TimeSpan contentLength, string compilationPath, string title)
        {
            VideoManager videoManager = new VideoManager();

            TimeSpan compilationLength = videoManager.GetVideoLength(compilationPath);
            // time difference (in seconds) between the actual content length and the invalid contentLength
            double timeDifference = compilationLength
                .Subtract(UseOutro ? videoManager.GetVideoLength(OutroPath) : new TimeSpan())
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
                contentLength = contentLength.Add(videoManager.GetVideoLength(clip.LocalLocation));
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

        protected void StoreUploadEntryToDb(string title, TimePeriod timePeriod, DateTime date)
        {
            using (var db = new ContentReuploadContext())
            {
                db.YoutubeUploads.Add(new YoutubeUpload
                {
                    Channel = ChannelName,
                    Title = title,
                    TimePeriod = timePeriod.ToString(),
                    Date = date
                });
                db.SaveChanges();
            }
        }

        protected DateTime GetLastUploadDate(TimePeriod timePeriod)
        {
            using (var db = new ContentReuploadContext())
            {
                var lastUpload = db.YoutubeUploads.ToList().LastOrDefault
                    (x => x.Channel == ChannelName && x.TimePeriod == timePeriod.ToString());
                if (lastUpload != null)
                {
                    return lastUpload.Date;
                }
            }
            return new DateTime();
        }

        protected int GetUploadsCount(TimePeriod timePeriod)
        {
            using (var db = new ContentReuploadContext())
            {
                var uploads = db.YoutubeUploads.ToList().Where
                    (x => x.Channel == ChannelName && x.TimePeriod == timePeriod.ToString());
                if (uploads != null)
                {
                    return uploads.Count();
                }
            }
            return 0;
        }
    }
}
