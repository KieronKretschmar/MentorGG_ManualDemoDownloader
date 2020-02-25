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
        [Ignore]
        [TestMethod]
        public void ControllerRequestCreatesNewBlobInStorage()
        {
            var testBlobConnectionString = "UseDevelopmentStorage=true";
            var testBlobStorage = new BlobStorage(testBlobConnectionString, new TestLogger<BlobStorage>());
            var mockDemoCentral = new Mock<IDemoCentral>();

            var testDemoPath = @"C:\Users\Lasse\source\repos\ManualDemoDownloader\ManualDemoDownloaderTests\TestData\TestDemo_Valve2.dem.bz2";
            string testDemoFileName = Path.GetFileName(testDemoPath);
            var testSteamId = 1234;
            
            var test = new ManualDemoDownloadController(new TestLogger<ManualDemoDownloadController>(), testBlobStorage, mockDemoCentral.Object);

            //SET UP Request
            HttpRequestMessage testRequest = new HttpRequestMessage(HttpMethod.Post,"test-uri");
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new ByteArrayContent(File.ReadAllBytes(testDemoPath)));

            testRequest.Content = multipartFormDataContent;
            testRequest.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data");
            testRequest.Content.Headers.ContentDisposition.FileName = testDemoFileName;

            test.Request = testRequest;
            
            //ACT
            test.PostDemo(testSteamId).Wait();


            //ASSERT 
            //New blob is created
            var blob = new BlobServiceClient(testBlobConnectionString).GetBlobContainerClient("manual-upload").GetBlobClient(testDemoFileName);

            Assert.IsTrue(blob.Exists());

            //DemoCentral is called
            mockDemoCentral.Verify(x => x.PublishMessage(It.IsAny<string>(), It.IsAny<GathererTransferModel>()), Times.Once);
        }
    }
}
