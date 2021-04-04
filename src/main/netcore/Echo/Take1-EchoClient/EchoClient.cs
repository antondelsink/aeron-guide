using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Adaptive.Aeron;
using Adaptive.Aeron.LogBuffer;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;

namespace Take1_EchoClient
{
    public sealed class EchoClient : IDisposable
    {
        public static TextWriter Log = null;

        private static readonly int ECHO_STREAM_ID = 0x2044f002;

        private readonly Aeron aeron;
        private readonly IPEndPoint local_address;
        private readonly IPEndPoint remote_address;

        public EchoClient(
            Aeron in_aeron,
            IPEndPoint in_local_address,
            IPEndPoint in_remote_address)
        {
            this.aeron = in_aeron ?? throw new ArgumentNullException(nameof(in_aeron));
            this.local_address = in_local_address ?? throw new ArgumentNullException(nameof(in_local_address));
            this.remote_address = in_remote_address ?? throw new ArgumentNullException(nameof(in_remote_address));
        }

        public static EchoClient Create(DirectoryInfo media_directory, IPEndPoint local_address, IPEndPoint remote_address)
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

            return new EchoClient(aeron, local_address, remote_address);
        }

        public void Run()
        {
            using (var sub = this.SetupSubscription())
            using (var pub = this.SetupPublication())
            {
                RunLoop(sub, pub);
            }
        }

        private void RunLoop(Subscription sub, Publication pub)
        {
            var buffer = new UnsafeBuffer(BufferUtil.AllocateDirectAligned(2048, 16));

            var random = new Random();

            while (true)
            {
                if (pub.IsConnected)
                {
                    if (SendMessage(pub, buffer, "HELLO " + this.local_address.Port))
                    {
                        break;
                    }
                }
            }

            var assembler = new FragmentAssembler(OnParseMessage);

            while (true)
            {
                if (pub.IsConnected)
                {
                    SendMessage(pub, buffer, random.Next(0, int.MaxValue).ToString());
                }
                if (sub.IsConnected)
                {
                    sub.Poll(assembler, 10);
                }
                Thread.Sleep(1000);
            }
        }

        private static void OnParseMessage(IDirectBuffer buffer, int offset, int length, Header header)
        {
            byte[] buf = new byte[length];
            buffer.GetBytes(offset, buf);
            var response = Encoding.UTF8.GetString(buf);

            Log?.WriteLine($"response: {response}");
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

            return this.aeron.AddPublication(pub_uri, ECHO_STREAM_ID);
        }
        private Subscription SetupSubscription()
        {
            var sub_uri = new ChannelUriStringBuilder()
                .Reliable(true)
                .Media("udp")
                .Endpoint(this.local_address.ToString())
                .Build();

            return this.aeron.AddSubscription(sub_uri, ECHO_STREAM_ID);
        }

        public void Dispose()
        {
            aeron?.Dispose();
        }
    }
}