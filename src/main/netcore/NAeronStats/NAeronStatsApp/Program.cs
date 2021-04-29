using System;
using System.IO;
using System.Linq;

namespace NAeron
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(@"Usage: NAeronStatsApp ""filename.ext""");
                return -1;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"Specified File Does Not Exist: {args[0]}");
                return -2;
            }

            using (var nas = new NAeronStats(args[0]))
            {
                var headerLine1 = string.Format("{0,70}", "NAeronStats");
                var headerLine2 = string.Empty.PadLeft(headerLine1.Length, '=');

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine(headerLine1);
                    Console.WriteLine(headerLine2);

                    var stats = from stat in nas.GetStats()
                                where stat.Item1.TypeID == 0 // System Counters == 0
                                select stat;

                    foreach (var stat in stats)
                    {
                        Console.WriteLine($"{stat.Item1.Label,70}: {stat.Item2}");
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}