using ContentReupload.App.Models;
using ContentReupload.Reddit.Models;
using System;
using System.Linq;

namespace ContentReupload.App.Db
{
    public class DatabaseUtil
    {
        public void StoreUploadEntry(string channelName, string title, TimePeriod timePeriod, DateTime date)
        {
            if (string.IsNullOrEmpty(channelName) || string.IsNullOrEmpty(title) || date == null || date == default)
                throw new ArgumentException();

            using (var db = new ContentReuploadContext())
            {
                db.YoutubeUploads.Add(new YoutubeUpload
                {
                    Channel = channelName,
                    Title = title,
                    TimePeriod = timePeriod.ToString(),
                    Date = date
                });

                db.SaveChanges();
            }
        }

        public DateTime GetLastUploadDate(string channelName, TimePeriod timePeriod)
        {
            using (var db = new ContentReuploadContext())
            {
                var lastUpload = db.YoutubeUploads.ToList().LastOrDefault
                    (x => x.Channel == channelName && x.TimePeriod == timePeriod.ToString());

                if (lastUpload != null)
                {
                    return lastUpload.Date;
                }
            }

            return new DateTime();
        }

        public int GetUploadsCount(string channelName, TimePeriod timePeriod)
        {
            using (var db = new ContentReuploadContext())
            {
                var uploads = db.YoutubeUploads.ToList().Where
                    (x => x.Channel == channelName && x.TimePeriod == timePeriod.ToString());

                if (uploads != null)
                {
                    return uploads.Count();
                }
            }

            return 0;
        }
    }
}
