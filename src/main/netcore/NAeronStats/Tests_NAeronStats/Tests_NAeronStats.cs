using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Adaptive.Agrona.Concurrent.Status;

using NAeron;
using System.Linq;

namespace Tests_NAeron
{
    [TestClass]
    public class Tests_NAeronStats
    {
        private const string cncFileName = @"C:\Users\Administrator\AppData\Local\Temp\2\aeron-Administrator\cnc.dat";

        private static Adaptive.Aeron.Aeron aa;
        private static CountersReader cr;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            // Reference Implementation
            aa = Adaptive.Aeron.Aeron.Connect();
            cr = aa.CountersReader;
        }

        [TestMethod]
        public void Test_01_New()
        {
            Assert.IsTrue(File.Exists(cncFileName), $"Specified CNC File Does Not Exist: {cncFileName}");

            using (var nas = new NAeronStats(cncFileName))
            {
                Assert.IsTrue(cr.MetaDataBuffer.Capacity == nas.MetaData.CountersMetadataBufferLength, "CountersMetaData size obtained by reference implementation does not match size obtained by NAeronStats");
                Assert.IsTrue(cr.ValuesBuffer.Capacity == nas.MetaData.CountersValuesBufferLength, "CountersValues size obtained by reference implementation does not match size obtained by NAeronStats");
            }
        }

        [TestMethod]
        public void Test_02_GetCounterMetaData()
        {
            Assert.IsTrue(File.Exists(cncFileName), $"Specified CNC File Does Not Exist: {cncFileName}");

            using (var nas = new NAeronStats(cncFileName))
            {
                Assert.IsTrue(nas.GetCounterMetaData().Count() > 0);

                foreach (var c in nas.GetCounterMetaData())
                {
                    Debug.WriteLine(c.ToString());
                }
            }
        }

        [TestMethod]
        public void Test_03_GetCounterValues()
        {
            Assert.IsTrue(File.Exists(cncFileName), $"Specified CNC File Does Not Exist: {cncFileName}");

            using (var nas = new NAeronStats(cncFileName))
            {
                Assert.IsTrue(nas.GetCounterValues().Count() > 0);

                int ix = 0;
                foreach (var s in nas.GetCounterValues())
                {
                    Debug.WriteLine($"Counter: {ix} Value: {s}");
                    ix++;
                }
            }
        }

        [TestMethod]
        public void Test_04_GetCounter()
        {
            Assert.IsTrue(File.Exists(cncFileName), $"Specified CNC File Does Not Exist: {cncFileName}");

            using (var nas = new NAeronStats(cncFileName))
            {
                int ix = 0;
                foreach (var c in nas.GetCounterMetaData())
                {
                    Debug.WriteLine($"Label: {c.Label} Value: {nas[ix]}");
                    ix++;
                }
            }
        }
    }
}
