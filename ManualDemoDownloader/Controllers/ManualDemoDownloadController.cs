﻿using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.TransferModels;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Microsoft.AspNetCore.Mvc;
using ManualDemoDownloader.Models;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace ManualUpload.Controllers
{
    [Route("v{version:apiVersion}/demo")]
    public class ManualDemoDownloadController : Controller
    {
        public static readonly List<string> AllowedFileExtensions = new List<string>
        {
            ".dem",
            ".bz2",
            ".gz",
            ".zip",
        };

        public static readonly int MaxFilesPerUpload = 5;

        private readonly ILogger<ManualDemoDownloadController> _logger;
        private readonly IBlobStorage _blobStorage;
        private readonly IProducer<ManualDownloadReport> _demoEntry;

        public ManualDemoDownloadController(
            ILogger<ManualDemoDownloadController> logger,
            IBlobStorage blobStorage,
            IProducer<ManualDownloadReport> demoEntry)
        {
            _logger = logger;
            _blobStorage = blobStorage;
            _demoEntry = demoEntry;
        }

        [HttpPost]
        // POST api/v{version}/demo
        public async Task<ActionResult<UploadResultModel>> ReceiveDemoAsync([FromForm]long steamId, [FromForm]IFormFileCollection demos)
        {
            if (steamId == 0)
            {
                _logger.LogWarning("Received POST without SteamId specified");
                return StatusCode(400);
            }

            if (demos == null || demos.Count == 0)
            {
                _logger.LogWarning("Received POST without Demos specified");
                return StatusCode(400);
            }
            else if (demos.Count > MaxFilesPerUpload)
            {
                _logger.LogWarning($"Received POST without too many Demos specified, Maximum is [ {MaxFilesPerUpload}]");
                return StatusCode(400);
            }

            _logger.LogInformation($"Receiving Demo(s) associated with SteamId: [ {steamId} ]");

            int successfulCount = 0;
            foreach (var demo in demos)
            {
                string ext = Path.GetExtension(demo.FileName);
                if (!AllowedFileExtensions.Contains(ext))
                {
                    _logger.LogWarning($"Skipping file with disallowed file extension [ {ext} ]");
                    continue;
                }

                string blobName = Guid.NewGuid().ToString() + ext;

                string blobLocation = await _blobStorage.UploadBlobAsync(blobName, demo.OpenReadStream());

                if (blobLocation == null)
                {
                    _logger.LogWarning($"Failed to retrieve BlobStorage location, skipping this demo");
                    continue;
                }

                var model = new ManualDownloadReport
                {
                    BlobUrl = blobLocation,
                    MatchDate = DateTime.UtcNow,
                    UploadDate = DateTime.UtcNow,
                    UploaderId = steamId,
                    Source = Source.ManualUpload,
                    UploadType = UploadType.ManualUserUpload
                };

                _demoEntry.PublishMessage(model);

                successfulCount++;
            }

            _logger.LogInformation($"[ {successfulCount} ] New upload(s) from SteamId: [ {steamId} ]");
            return new UploadResultModel{ DemoCount = successfulCount };
        }


        /// <summary>
        /// Returns the full file ending of the filePath, including double endings like ".dem.gz".
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string GetFullFileEnding(string filePath)
        {
            return Regex.Match(filePath, @"\..*").Value;
        }
    }
}
