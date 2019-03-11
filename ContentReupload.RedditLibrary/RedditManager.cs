using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContentReupload.RedditLibrary
{
    public class RedditManager
    {
        private readonly Reddit _reddit;

        public RedditManager()
        {
            _reddit = new Reddit();
        }

        public async Task<List<RedditSubmission>> ObtainTopAsync(string subName, string domain, TimePeriod timePeriod, 
            bool orderByPoints, bool infoMessages = true)
        {
            if (subName == null)
                throw new ArgumentNullException();

            if (infoMessages)
                Console.WriteLine($"Obtaining {timePeriod.ToString().ToLower()} top posts of /r/{subName} with domain '{domain}'");

            try
            {
                Subreddit sub = await _reddit.GetSubredditAsync(subName);

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

                List<Post> topList = top
                    .Where(x => x != null && !x.NSFW && x.Domain == domain && x.BannedBy == null && !x.IsArchived
                    && (x.ModReports == null || x.ModReports.Count == 0) && (x.LinkFlairText == null || !x.LinkFlairText.ToLower().Contains("warning"))
                    && (x.IsRemoved == null || x.IsRemoved == false) && (x.ReportCount == null || x.ReportCount == 0))
                    .ToList();

                if (orderByPoints)
                    topList = topList.OrderByDescending(x => x.Score).ToList();

                List<RedditSubmission> redditSubmissions = topList.Select(x => new RedditSubmission
                {
                    Title = x.Title,
                    Url = x.Url
                }).ToList();

                if (infoMessages)
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

        public async Task<Dictionary<string, List<RedditSubmission>>> GetSubsAsync(string domain)
        {
            Dictionary<string, List<RedditSubmission>> result = new Dictionary<string, List<RedditSubmission>>();
            var popular = _reddit.GetPopularSubreddits().ToList();
            foreach(var sub in popular)
            {
                var posts = await ObtainTopAsync(sub.Name, domain, TimePeriod.Day, false, false);
                if (posts.Count >= 10)
                {
                    result.Add(sub.Name, posts);
                }
            }
            return result;
        }
    }
}
