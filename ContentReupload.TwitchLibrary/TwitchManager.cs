using ContentReupload.Util;
using ContentReupload.VideoLibrary;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace ContentReupload.TwitchLibrary
{
    public class TwitchManager
    {
        private VideoManager videoManager = new VideoManager();

        public TwitchClip DownloadClip(string title, string clipPath)
        {
            if (title == null || clipPath == null)
                throw new ArgumentNullException();

            string downloadLocation = null;

            string clipsDomain = "clips.twitch.tv";
            if (clipPath.Contains(clipsDomain))
            {
                clipPath = clipPath.Substring(clipPath.IndexOf(clipsDomain) + clipsDomain.Length);
            }

            Console.WriteLine($"Downloading twitch clip \"{title}\" ({clipPath})");

            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");

            string driverPath = AppDomain.CurrentDomain.BaseDirectory
                .Replace("ContentReupload.App", "ContentReupload.TwitchLibrary");

            try
            {
                string channel = null;
                string link = null;

                using (var browser = new ChromeDriver(driverPath, chromeOptions))
                {
                    browser.Navigate().GoToUrl("https://clipr.xyz" + clipPath);

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

                using (var client = new WebClient())
                {
                    downloadLocation = UtilMethods.GetDocumentsPath() + "/YouTube/Clips";
                    if (!Directory.Exists(downloadLocation))
                    {
                        Directory.CreateDirectory(downloadLocation);
                    }

                    downloadLocation += "/" + videoManager.ValidateFileName(title) + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".mp4";

                    client.DownloadFile(link, downloadLocation);

                    Console.WriteLine($"Completed downloading twitch clip \"{clipPath}\"");
                }

                return new TwitchClip
                {
                    Title = title,
                    Channel = channel,
                    LocalLocation = downloadLocation
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("Twitch download error: " + e.Message);
                return null;
            }
        }
    }
}
