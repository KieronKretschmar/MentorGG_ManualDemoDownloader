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

namespace ManualDemoDownloaderTests
{
    [TestClass]
    public class IntegrationTests
    {

        [TestMethod]
        [Ignore]
        public void ControllerRequestCreatesNewBlobInStorage()
        {
            
        }
    }
}
