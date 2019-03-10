using ContentReupload.App.Models;
using System.Data.Entity;

namespace ContentReupload.App
{
    public class ContentReuploadContext : DbContext
    {
        public DbSet<YoutubeUpload> YoutubeUploads { get; set; }
        
        public ContentReuploadContext() : base("ContentReuploadDatabase")
        {

        }
    }
}
