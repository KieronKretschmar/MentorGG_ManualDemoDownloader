using System;
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
        Task<string> UploadBlobAsync(string blobName, Stream content);
    }

    public class BlobStorage : IBlobStorage
    {
        ILogger<BlobStorage> _logger;
        private readonly BlobContainerClient _containerClient;
        private static string CONTAINER_NAME = "manual-upload";


        /// <summary>
        /// Connects to blob storage and creates a blob container.
        /// </summary>
        /// <param name="configuration"></param>
        public BlobStorage(string connectionString, ILogger<BlobStorage> logger)
        {
            _logger = logger;

            var client = new BlobContainerClient(connectionString, CONTAINER_NAME);
            client.CreateIfNotExists(PublicAccessType.Blob);
            _containerClient = client;
        }

        /// <summary>
        /// Stream content to Blob Storage.
        /// </summary>
        public async Task<string> UploadBlobAsync(string blobName, Stream content)
        {
            var client = _containerClient.GetBlobClient(blobName);
            await _containerClient.UploadBlobAsync(blobName, content);
            _logger.LogInformation($"Uploaded [ {blobName} ] to [ {client.Uri.AbsoluteUri} ]");
            return client.Uri.AbsoluteUri;
        }
    }
}
