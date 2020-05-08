using System;

namespace ContentReupload.Common.Models
{
    public class VideoClip
    {
        public string Channel { get; set; }
        public string Title { get; set; }
        public TimeSpan Length { get; set; }
        public string LocalLocation { get; set; }
        public bool IsEngagementClip { get; set; }
    }
}
