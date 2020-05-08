using ContentReupload.Common;
using ContentReupload.Common.Models;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace ContentReupload.Twitch
{
    public class TwitchDownloader
    {
        public readonly string ClipDomain = "clips.twitch.tv";

        private readonly string _downloadFolder;

        public TwitchDownloader(string downloadFolder)
        {
            _downloadFolder = downloadFolder;
        }

        public VideoClip DownloadClip(string title, string clipId)
        {
            if (title == null || clipId == null)
                throw new ArgumentNullException();

            if (clipId.Contains(ClipDomain))
            {
                clipId = clipId.Substring(clipId.IndexOf(ClipDomain) + ClipDomain.Length);
            }

            Console.WriteLine($"Downloading twitch clip \"{title}\" ({clipId})");

            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");

            try
            {
                string channel = null;
                string link = null;

                using (var browser = new ChromeDriver(chromeOptions))
                {
                    browser.Navigate().GoToUrl("https://clipr.xyz" + clipId);

                    var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(30));
                    
                    #pragma warning disable 612, 618 // ignores ExpectedConditions obsolete warning
                    wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("clipr-button")));
                    #pragma warning restore 612, 618

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(browser.PageSource);

                    channel = doc.DocumentNode.SelectSingleNode("//small[@class='text-muted']").InnerText.Trim();

                    link = doc.DocumentNode.SelectNodes("//a")
                        .Select(x => x.GetAttributeValue("href", string.Empty))
                        .FirstOrDefault(x => x.EndsWith(".mp4"));
                }

                if (channel == null || link == null)
                {
                    Console.WriteLine("Failed to extract channel/link from twitch clip using clipr");
                    return null;
                }

                string downloadLocation = null;

                using (var client = new WebClient())
                {
                    if (!Directory.Exists(_downloadFolder))
                    {
                        Directory.CreateDirectory(_downloadFolder);
                    }

                    var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    downloadLocation = _downloadFolder + "/" + FileUtil.ValidateFileName(clipId) + "_" + unix.Substring(unix.Length - 5) + ".mp4";

                    client.DownloadFile(link, downloadLocation);

                    Console.WriteLine($"Completed downloading twitch clip \"{clipId}\"");
                }

                return new VideoClip
                {
                    Channel = channel,
                    Title = title,
                    LocalLocation = downloadLocation,
                    IsEngagementClip = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("[Twitch download error] " + Environment.NewLine + e.ToString());
                return null;
            }
        }
    }
}
