using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SUKOAuto.Tests
{
    [TestClass]
    public class SukoMachineUnitTests
    {
        [TestMethod]
        public void PlayListScanTest()
        {
            var ChromeOption = new ChromeOptions();
            ChromeDriver Chrome=null;
            try
            {
                Chrome = new ChromeDriver(ChromeOption);
                var movies = SukoSukoMachine.FindMoviesInPlayList(Chrome, "PLOzRBsDgSbxxZ5SjVplbuBB_BjcHZts74");
                // I wish it to contain enough... or it will fail...
                Assert.IsTrue(movies.Count()>200);
                // I wish it to contain... or it will fail...
                Assert.IsTrue(movies.Contains("sSLs8T_Xyss"));
            }
            finally
            {
                if (Chrome!=null)
                    Chrome.Dispose();
            }
        }
    }
}
