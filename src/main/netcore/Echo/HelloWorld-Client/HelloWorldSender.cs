using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Adaptive.Aeron;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;

namespace HelloWorld_Client
{
    public sealed class HelloWorldSender : IDisposable
    {
        public static TextWriter Log = null;

        private static readonly int HELLOWORLD_STREAM_ID = 0x2044f002;

        private readonly Aeron aeron;
        private readonly IPEndPoint remote_address;

        public HelloWorldSender(
            Aeron in_aeron,
            IPEndPoint in_remote_address)
        {
            this.aeron = in_aeron ?? throw new ArgumentNullException(nameof(in_aeron));
            this.remote_address = in_remote_address ?? throw new ArgumentNullException(nameof(in_remote_address));
        }

        public static HelloWorldSender Create(DirectoryInfo media_directory, IPEndPoint remote_address)
        {
            string directory = media_directory is not null && media_directory.Exists ? media_directory.FullName : throw new DirectoryNotFoundException();

            var aeron_context = new Aeron.Context().AeronDirectoryName(directory);

            Aeron aeron = null;
            try
            {
                aeron = Aeron.Connect(aeron_context);
            }
            catch (Exception)
            {
                aeron?.Dispose();
                throw;
            }

            return new HelloWorldSender(aeron, remote_address);
        }

        public void Run()
        {
            using (var pub = this.SetupPublication())
            {
                RunLoop(pub);
            }
        }

        private void RunLoop(Publication pub)
        {
            var buffer = new UnsafeBuffer(BufferUtil.AllocateDirectAligned(2048, 16));

            while (true)
            {
                if (pub.IsConnected)
                {
                    SendMessage(pub, buffer, "HELLO");
                    SendMessage(pub, buffer, "WORLD");
                    break;
                }
            }
        }

        private static bool SendMessage(Publication pub, UnsafeBuffer buffer, string text)
        {
            buffer.PutBytes(0, Encoding.UTF8.GetBytes(text));

            long result = 0;
            for (int ix = 0; ix < 5; ++ix)
            {
                result = pub.Offer(buffer, 0, text.Length);
                if (result < 0)
                {
                    try
                    {
                        Thread.Sleep(100);
                    }
                    catch (ThreadInterruptedException)
                    {
                        Thread.CurrentThread.Interrupt();
                    }
                    continue;
                }
                return true;
            }

            Log?.WriteLine($"could not send: {result}");
            return false;
        }

        private Publication SetupPublication()
        {
            var pub_uri = new ChannelUriStringBuilder()
                .Reliable(true)
                .Media("udp")
                .Endpoint(this.remote_address.ToString())
                .Build();

            return this.aeron.AddPublication(pub_uri, HELLOWORLD_STREAM_ID);
        }

        public void Dispose()
        {
            aeron?.Dispose();
        }
    }
}