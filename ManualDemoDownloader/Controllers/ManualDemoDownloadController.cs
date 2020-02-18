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
    [Route("v{version:apiVersion}/public")]
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
        private readonly ILogger<ManualDemoDownloadController> _logger;
        private readonly IBlobStorage _blobStorage;
        private readonly IDemoCentral _demoCentral;
        private readonly string _tempDirectory;

        public ManualDemoDownloadController(ILogger<ManualDemoDownloadController> logger, IBlobStorage blobStorage, IConfiguration configuration, IDemoCentral demoCentral)
        {
            _logger = logger;
            _blobStorage = blobStorage;
            _demoCentral = demoCentral;
            _tempDirectory = configuration.GetValue<string>("TEMP_DIRECTORY");
        }

        [HttpPost("Manual")]
        // POST api//trusted/Upload/Manual
        public async Task<ActionResult> PostDemo()
        {
            var playerId = long.Parse(User.Identity.Name);

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

                    // Rename by adding original file extension
                    var newFilePath = localFilePath + fileExtension;
                    File.Move(localFilePath, newFilePath);
                    await _blobStorage.UploadToBlob(file.LocalFileName, newFilePath);

                    var model = new GathererTransferModel
                    {
                        DownloadUrl = newFilePath,
                        MatchDate = DateTime.UtcNow,
                        UploaderId = playerId,
                        Source = Source.ManualUpload,
                        UploadType = UploadType.ManualUserUpload
                    };

                    _demoCentral.PublishMessage(new Guid().ToString(), model);
                }

                _logger.LogInformation($"New manual upload from {playerId}");
                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not upload manually from {playerId}, because of {e.Message}", e);
                return new StatusCodeResult(500);
            }
        }
    }
}
