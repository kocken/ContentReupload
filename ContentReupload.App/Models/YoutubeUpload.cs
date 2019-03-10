using System;
using System.ComponentModel.DataAnnotations;

namespace ContentReupload.App.Models
{
    public class YoutubeUpload
    {
        [Key]
        public int Id { get; set; }

        public string Channel { get; set; }
        public string Title { get; set; }
        public string TimePeriod { get; set; }
        public DateTime Date { get; set; }
    }
}
