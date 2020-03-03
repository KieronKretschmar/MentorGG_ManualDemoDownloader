using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.TransferModels;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Microsoft.AspNetCore.Mvc;

namespace ManualUpload.Controllers
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/demo")]
    [ApiController]
    public class ManualDemoDownloadController : BaseApiController
    {
        public static readonly List<string> AllowedFileExtensions = new List<string>
        {
            ".bz2",
            ".dem",
            ".gz",
            ".zip",
        };

        public static readonly int MaxFilesPerUpload = 5;
        private readonly string _tempDirectory = "/tmp";

        private readonly ILogger<ManualDemoDownloadController> _logger;
        private readonly IBlobStorage _blobStorage;
        private readonly IProducer<DemoEntryInstructions> _demoEntry;

        public ManualDemoDownloadController(
            ILogger<ManualDemoDownloadController> logger,
            IBlobStorage blobStorage,
            IProducer<DemoEntryInstructions> demoEntry)
        {
            _logger = logger;
            _blobStorage = blobStorage;
            _demoEntry = demoEntry;
        }

        [HttpPost]
        // POST api/v{version}/demo
        public async Task<ActionResult> ReceiveDemoAsync([FromForm]long steamId)
        {
            if (steamId == 0)
            {
                _logger.LogWarning("Received POST without SteamId specified");
                return new BadRequestResult();
            }

            _logger.LogInformation($"Receiving Demo associated with SteamId: [ {steamId} ]");
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                return new UnsupportedMediaTypeResult();
            }

            Directory.CreateDirectory(_tempDirectory);
            var provider = new MultipartFormDataStreamProvider(_tempDirectory);

            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                // Check if the request contains too many matches
                if (provider.FileData.Count > MaxFilesPerUpload)
                {
                    return new BadRequestResult();
                }

                // This illustrates how to get the file names.
                foreach (MultipartFileData file in provider.FileData)
                {
                    var localFilePath = Path.Combine(_tempDirectory, file.LocalFileName);

                    // Abort if file extension is not supported
                    var fileExtension = Path.GetExtension(file.Headers.ContentDisposition.FileName);
                    if (!AllowedFileExtensions.Contains(fileExtension))
                    {
                        File.Delete(localFilePath);
                        continue;
                    }

                    var filePathWithExtension = localFilePath + fileExtension;
                    var blobLocation = await _blobStorage.UploadToBlob(Path.GetFileName(filePathWithExtension), localFilePath);

                    var model = new DemoEntryInstructions
                    {
                        DownloadUrl = blobLocation,
                        MatchDate = DateTime.UtcNow,
                        UploaderId = steamId,
                        Source = Source.ManualUpload,
                        UploadType = UploadType.ManualUserUpload
                    };

                    _demoEntry.PublishMessage(new Guid().ToString(), model);
                }

                _logger.LogInformation($"New manual upload from SteamId: [ {steamId} ]");
                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not upload manually from SteamId: [ {steamId} ], because of {e.Message}", e);
                return new StatusCodeResult(500);
            }
        }
    }
}
