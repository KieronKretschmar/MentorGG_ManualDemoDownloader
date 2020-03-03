using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ManualUpload.Communication;
using RabbitCommunicationLib.TransferModels;
using RabbitCommunicationLib.Enums;

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
        private readonly IDemoCentral _demoCentral;

        public ManualDemoDownloadController(ILogger<ManualDemoDownloadController> logger, IBlobStorage blobStorage, IDemoCentral demoCentral)
        {
            _logger = logger;
            _blobStorage = blobStorage;
            _demoCentral = demoCentral;
        }

        [HttpPost]
        // POST api/v{version}/demo/<steamId>
        public async Task<ActionResult> ReceiveDemoAsync(long steamId)
        {
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

                    var model = new GathererTransferModel
                    {
                        DownloadUrl = blobLocation,
                        MatchDate = DateTime.UtcNow,
                        UploaderId = steamId,
                        Source = Source.ManualUpload,
                        UploadType = UploadType.ManualUserUpload
                    };

                    _demoCentral.PublishMessage(new Guid().ToString(), model);
                }

                _logger.LogInformation($"New manual upload from {steamId}");
                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not upload manually from {steamId}, because of {e.Message}", e);
                return new StatusCodeResult(500);
            }
        }
    }
}
