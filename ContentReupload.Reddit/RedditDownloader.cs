using ContentReupload.Reddit.Models;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContentReupload.Reddit
{
    public class RedditDownloader
    {
        private readonly bool _log = true;

        private readonly RedditSharp.Reddit _redditSharp;

        public RedditDownloader()
        {
            _redditSharp = new RedditSharp.Reddit();
        }

        public async Task<List<RedditSubmission>> ObtainTopAsync(string subName, string domain, TimePeriod timePeriod, bool orderByPoints)
        {
            if (subName == null)
                throw new ArgumentNullException();

            if (_log)
                Console.WriteLine($"Obtaining {timePeriod.ToString().ToLower()} top posts of /r/{subName} with domain '{domain}'");

            try
            {
                Subreddit sub = await _redditSharp.GetSubredditAsync(subName);

                Listing<Post> top = null;

                switch (timePeriod)
                {
                    case TimePeriod.All:
                        top = sub.GetTop(FromTime.All);
                        break;

                    case TimePeriod.Year:
                        top = sub.GetTop(FromTime.Year);
                        break;

                    case TimePeriod.Month:
                        top = sub.GetTop(FromTime.Month);
                        break;

                    case TimePeriod.Week:
                        top = sub.GetTop(FromTime.Week);
                        break;

                    case TimePeriod.Day:
                        top = sub.GetTop(FromTime.Day);
                        break;

                    case TimePeriod.Hour:
                        top = sub.GetTop(FromTime.Hour);
                        break;
                }

                List<Post> topList = top.Where(x => x != null && x.Domain == domain).ToList(); // valid posts

                topList = topList. // filter content that potentially might not be advertiser-friendly
                    Where(x =>
                    x.IsRemoved != true && !x.NSFW && !x.IsArchived &&
                    x.LinkFlairText?.ToLower().Contains("warning") != true)
                    .ToList();

                if (orderByPoints)
                    topList = topList.OrderByDescending(x => x.Score).ToList();

                List<RedditSubmission> redditSubmissions = topList.Select(x => new RedditSubmission
                {
                    Title = x.Title,
                    Url = x.Url
                }).ToList();

                if (_log)
                    Console.WriteLine($"Gathered {redditSubmissions.Count} posts from /r/{subName}");

                return redditSubmissions;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return new List<RedditSubmission>();
        }

        public async Task<Dictionary<string, List<RedditSubmission>>> FindSubsAsync(string domain)
        {
            var result = new Dictionary<string, List<RedditSubmission>>();
            var popular = _redditSharp.GetPopularSubreddits().ToList();

            foreach (var sub in popular)
            {
                var posts = await ObtainTopAsync(sub.Name, domain, TimePeriod.Day, true);
                if (posts.Count >= 10)
                {
                    result.Add(sub.Name, posts);
                }
            }

            return result;
        }
    }
}
