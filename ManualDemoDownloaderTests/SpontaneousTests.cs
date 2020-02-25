using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace ManualDemoDownloaderTests
{
    [TestClass]
    public class SpontaneousTests
    {
        [TestMethod]
        public void LoggerTest()
        {
            Logger.LogMessage("Where does this end up?");
            //It ends up in the test runner under a link to the additional ressources
        } 
    }
}
