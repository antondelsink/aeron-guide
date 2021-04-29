using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace NAeron
{
    [TestClass]
    public class Tests_WindowsPerfCounters
    {
        private readonly string cncFileName = Path.Combine(Adaptive.Aeron.Aeron.Context.GetAeronDirectoryName(), @"cnc.dat");

        [TestMethod]
        public void Test_WindowsPerformanceCounters()
        {
            var testDuration = TimeSpan.FromSeconds(180);

            Assert.IsTrue(OperatingSystem.IsWindows());
            if (!OperatingSystem.IsWindows())
                return;

            var pccName = "Aeron";
            DeleteIfWindowsPerformanceCounterCategoryExists(pccName);

            Assert.IsTrue(File.Exists(cncFileName), $"Specified CNC File Does Not Exist. Check if the Media Driver is Running. File: {cncFileName}");
            using (var nas = new NAeronStats(cncFileName))
            {
                var counters = from cmd in nas.GetCounterMetaData()
                               where cmd.TypeID == 0
                               select cmd;

                CreateWindowsPerformanceCounters(pccName, counters);

                var perfCounters = new Dictionary<int, PerformanceCounter>();
                LoadWindowsPerformanceCounters(pccName, counters, perfCounters);

                var timer = Stopwatch.StartNew();
                while (timer.Elapsed < testDuration)
                {
                    for (int ixCounter = 0; ixCounter < perfCounters.Count; ixCounter++)
                    {
                        perfCounters[ixCounter].RawValue = nas.GetCounterValue(ixCounter);
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private static void LoadWindowsPerformanceCounters(string pccName, IEnumerable<NAeronCounterMetaData> counters, Dictionary<int, PerformanceCounter> perfCounters)
        {
            var ix = 0;
            foreach (var c in counters)
            {
                var pc = new PerformanceCounter(pccName, c.Label, readOnly: false);
                perfCounters.Add(ix, pc);
                ix++;
            }
        }

        private static void CreateWindowsPerformanceCounters(string pccName, IEnumerable<NAeronCounterMetaData> counters)
        {
            var ccdc = new CounterCreationDataCollection();
            foreach (var c in counters)
            {
                var ccd = new CounterCreationData(c.Label, string.Empty, PerformanceCounterType.NumberOfItems64);
                ccdc.Add(ccd);
            }
            PerformanceCounterCategory.Create(pccName, string.Empty, PerformanceCounterCategoryType.SingleInstance, ccdc);
        }

        private static void DeleteIfWindowsPerformanceCounterCategoryExists(string pccName)
        {
            if (PerformanceCounterCategory.Exists(pccName))
            {
                PerformanceCounterCategory.Delete(pccName);
            }
        }
    }
}