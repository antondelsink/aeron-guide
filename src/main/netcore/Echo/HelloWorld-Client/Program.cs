using System;
using System.IO;
using System.Net;

namespace HelloWorld_Client
{
    class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=== Sender ===");

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

            Console.WriteLine("===================");

            DirectoryInfo directory = new DirectoryInfo(args[0]);
            _ = new IPEndPoint(IPAddress.Parse(args[1]), int.Parse(args[2]));
            IPEndPoint remote_address = new IPEndPoint(IPAddress.Parse(args[3]), int.Parse(args[4]));

            HelloWorldSender.Log = Console.Out;

            using (var client = HelloWorldSender.Create(directory, remote_address))
            {
                client.Run();
            }

            return 0;
        }
    }
}
