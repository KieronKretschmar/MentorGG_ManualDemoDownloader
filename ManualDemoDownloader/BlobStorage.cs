﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ManualUpload
{
    public interface IBlobStorage
    {
        Task<string> UploadToBlob(string filePath);
        Task UploadToBlob(string blobName, string filePath);
    }

    class BlobStorage : IBlobStorage
    {
        ILogger<BlobStorage> _logger;
        private readonly BlobContainerClient _containerClient;
        private static string CONTAINER_NAME = "manual-upload";


        /// <summary>
        /// Connects to blob storage and creates a blob container.
        /// </summary>
        /// <param name="configuration"></param>
        public BlobStorage(IConfiguration configuration, ILogger<BlobStorage> logger)
        {
            string connectionString = configuration.GetValue<string>(
                "BLOB_CONNECTION_STRING")?? "UseDevelopmentStorage=true;";

            _logger = logger;

            var client = new BlobContainerClient(connectionString, CONTAINER_NAME);
            client.CreateIfNotExists();

            //TODO handle case in which container is already created
            //At least check if client is still a correct assignemnt or if the old one needs to be queried for

            _containerClient = client;
        }

        public async Task UploadToBlob(string blobName, string filePath)
        {
            var client = _containerClient.GetBlobClient(blobName);
            using (FileStream stream = File.OpenRead(filePath))
            {
                await client.UploadAsync(stream);
                _logger.LogInformation($"Uploaded {filePath} to blob#{blobName}");
            }
        }

        /// <summary>
        /// Upload a file to a new blob, created by a unique GUID.
        /// </summary>
        /// <returns>the GUID used as the blob's name</returns>
        public async Task<string> UploadToBlob(string filePath)
        {
            string guid = new Guid().ToString();
            await UploadToBlob(guid, filePath);
            return guid;
        }
    }
}