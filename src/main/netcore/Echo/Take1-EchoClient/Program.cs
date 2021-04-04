using System;
using System.IO;
using System.Net;

namespace Take1_EchoClient
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=== Echo Client ===");

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
            IPEndPoint local_address = new IPEndPoint(IPAddress.Parse(args[1]), int.Parse(args[2]));
            IPEndPoint remote_address = new IPEndPoint(IPAddress.Parse(args[3]), int.Parse(args[4]));

            EchoClient.Log = Console.Out;

            using (var client = EchoClient.Create(directory, local_address, remote_address))
            {
                client.Run();
            }

            return 0;
        }
    }
}
