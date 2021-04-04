using System;
using System.IO;
using System.Net;

namespace Take1_EchoServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=== Echo Server ===");

            if (args.Length < 5)
            {
                Console.WriteLine("usage: directory local-address local-port remote-address remote-port");
                return 1;
            }
            else
            {
                Console.WriteLine("Arguments:");
                for (int ix = 0; ix < args.Length; ix++)
                {
                    Console.WriteLine($"args[{ix}]: {args[ix]}");
                }
            }
            // C:\Users\Administrator\AppData\Local\Temp\2\aeron-Administrator
            Console.WriteLine("===================");
            Console.WriteLine(@"Directory Hint on Windows: default folder for cnc.dat looks similar to C:\Users\Administrator\AppData\Local\Temp\2\aeron-Administrator");
            Console.WriteLine("===================");

            var directory = new DirectoryInfo(args[0]);
            var local_address = new IPEndPoint(IPAddress.Parse(args[1]), int.Parse(args[2]));

            EchoServer.Log = Console.Out;

            using (var server = EchoServer.Create(directory, local_address))
            {
                server.Run();
            }

            return 0;
        }
    }
}
