using Microsoft.AspNetCore.Http;

namespace ManualDemoDownloader.Models
{
    public class UploadResultModel
    {
        /// <summary>
        /// Amount of Demos successfully stored.
        /// </summary>
        public int DemoCount { get; set; }
    }

}
