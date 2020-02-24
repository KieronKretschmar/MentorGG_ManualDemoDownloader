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
        Task<string> UploadToBlob(string filePath);
        Task<string> UploadToBlob(string blobName, string filePath);
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
        public BlobStorage(IConfiguration configuration, ILogger<BlobStorage> logger)
        {
            string connectionString = configuration.GetValue<string>(
                "BLOB_CONNECTION_STRING")?? "UseDevelopmentStorage=true;";

            _logger = logger;

            var client = new BlobContainerClient(connectionString, CONTAINER_NAME);
            client.CreateIfNotExists(PublicAccessType.Blob);

            _containerClient = client;
        }

        /// <summary>
        /// Upload a file to a blob
        /// </summary>
        /// <returns>the absolute uri to the blob</returns>
        public async Task<string> UploadToBlob(string blobName, string filePath)
        {
            var client = _containerClient.GetBlobClient(blobName);
            using (FileStream stream = File.OpenRead(filePath))
            {
                await client.UploadAsync(stream);
                _logger.LogInformation($"Uploaded {filePath} to blob#{blobName}");
            }

            return client.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Upload a file to a new blob, created by a unique GUID.
        /// </summary>
        /// <returns>the absolute uri to the blob</returns>
        public async Task<string> UploadToBlob(string filePath)
        {
            string guid = new Guid().ToString();
            return await UploadToBlob(guid, filePath);
        }
    }
}
