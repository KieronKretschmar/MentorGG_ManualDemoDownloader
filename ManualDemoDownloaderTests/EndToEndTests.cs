using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManualUpload.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc.Testing;
using ManualUpload;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using ManualUpload.Communication;
using System.Net.Http;
using System.IO;
using Azure.Storage.Blobs;
using RabbitCommunicationLib.TransferModels;

namespace ManualDemoDownloaderTests
{
    [TestClass]
    public class EndToEndTests
    {

        [TestMethod]
        public void ControllerRequestCreatesNewBlobInStorage()
        {
            IConfiguration testConfiguration = new ConfigurationBuilder().AddEnvironmentVariables("test_").Build();
            var testBlobStorage = new BlobStorage(testConfiguration, new TestLogger<BlobStorage>());
            var mockDemoCentral = new Mock<IDemoCentral>();

            var testDemoPath = @"C:\Users\Lasse\source\repos\ManualDemoDownloader\ManualDemoDownloaderTests\TestData\TestDemo_Valve2.dem.bz2";
            string testDemoFileName = Path.GetFileName(testDemoPath);
            
            var test = new ManualDemoDownloadController(new TestLogger<ManualDemoDownloadController>(), testBlobStorage, testConfiguration, mockDemoCentral.Object);

            //SET UP Request
            HttpRequestMessage testRequest = new HttpRequestMessage(HttpMethod.Post,"test-uri");
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new ByteArrayContent(File.ReadAllBytes(testDemoPath)));

            testRequest.Content = multipartFormDataContent;
            testRequest.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data");
            testRequest.Content.Headers.ContentDisposition.FileName = testDemoFileName;

            test.Request = testRequest;
            
            //ACT
            test.PostDemo().Wait();


            //ASSERT 
            //New blob is created
            var blob = new BlobServiceClient(testConfiguration.GetValue<string>("BLOB_CONNECTION_STRING"))
                .GetBlobContainerClient("manual-upload").GetBlobClient(testDemoFileName);

            Assert.IsTrue(blob.Exists());

            //DemoCentral is called
            mockDemoCentral.Verify(x => x.PublishMessage(It.IsAny<string>(), It.IsAny<GathererTransferModel>()), Times.Once);
        }
    }
}
