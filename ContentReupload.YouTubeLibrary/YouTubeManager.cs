using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ContentReupload.YouTubeLibrary
{
    public class YouTubeManager
    {
        public async Task<List<SearchResult>> GetVideosAsync(string youtubeChannel)
        {
            if (youtubeChannel == null)
                throw new ArgumentNullException();

            YouTubeService youtubeService = await GetYouTubeService(youtubeChannel);

            var channelsListRequest = youtubeService.Channels.List("contentDetails");
            channelsListRequest.Mine = true;
            var channelsListResponse = await channelsListRequest.ExecuteAsync();
            Channel channel = channelsListResponse.Items.FirstOrDefault();

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchListRequest.MaxResults = 20;
            searchListRequest.ChannelId = channel.Id;
            var searchListResponse = await searchListRequest.ExecuteAsync();

            return searchListResponse.Items.ToList();
        }

        public async Task UploadVideoAsync(string youtubeChannel, string title, string description, string[] tags, string videoFilePath)
        {
            if (youtubeChannel == null || title == null || description == null || tags == null || videoFilePath == null)
                throw new ArgumentNullException();

            Console.WriteLine($"Uploading youtube video \"{title}\"" +
                $" from path \"{videoFilePath}\" to youtube channel \"{youtubeChannel}\"");

            YouTubeService youtubeService = await GetYouTubeService(youtubeChannel);

            Video video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = title,
                    Description = description,
                    Tags = tags,
                    CategoryId = "20" // gaming, see https://developers.google.com/youtube/v3/docs/videoCategories/list
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = "public" // "unlisted", "private" or "public"
                }
            };

            using (FileStream fileStream = new FileStream(videoFilePath, FileMode.Open))
            {
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += VideosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += VideosInsertRequest_ResponseReceived;

                await videosInsertRequest.UploadAsync();
            }
        }

        public string OptimizeTitle(string title)
        {
            // #ToTitleCase format day dates wrongly, such as 21st to 21St
            // return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());

            string ev(Match m) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Value);
            return Regex.Replace(title, @"\b[a-zA-Z]+\b", ev);
        }

        private void VideosInsertRequest_ProgressChanged(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    Console.WriteLine("{0} bytes sent.", progress.BytesSent);
                    break;

                case UploadStatus.Failed:
                    Console.WriteLine("An error prevented the youtube upload from completing.\n{0}", progress.Exception);
                    break;
            }
        }

        private void VideosInsertRequest_ResponseReceived(Video video)
        {
            Console.WriteLine("Youtube video '{0}' was successfully uploaded.", video.Id);
        }

        private async Task<YouTubeService> GetYouTubeService(string youtubeChannel)
        {
            if (youtubeChannel == null)
                throw new ArgumentNullException();

            return new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = await GetCredentialAsync(youtubeChannel),
                ApplicationName = "YouTube uploader"
            });

        }

        private async Task<UserCredential> GetCredentialAsync(string youtubeChannel)
        {
            if (youtubeChannel == null)
                throw new ArgumentNullException();

            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube },
                    youtubeChannel,
                    CancellationToken.None,
                    new FileDataStore("YouTube uploader")
                );
            }
        }
    }
}
