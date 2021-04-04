using Adaptive.Aeron;
using Adaptive.Aeron.LogBuffer;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Take1_EchoServer
{
    public class EchoServer : IDisposable
    {
        public static TextWriter Log = null;

        private static readonly int ECHO_STREAM_ID = 0x2044f002;

        private readonly Aeron aeron;
        private readonly IPEndPoint local_address;
        private readonly Dictionary<int, ServerClient> clients;

        private EchoServer(Aeron in_aeron, IPEndPoint in_local_address)
        {
            this.aeron = in_aeron ?? throw new ArgumentNullException(nameof(in_aeron));
            this.local_address = in_local_address ?? throw new ArgumentNullException(nameof(in_local_address));

            this.clients = new Dictionary<int, ServerClient>(32);
        }

        private class ServerClient : IDisposable
        {
            private static readonly Regex HELLO_PATTERN = new Regex("HELLO ([0-9]+)");

            private enum State
            {
                INITIAL,
                CONNECTED
            }

            private int session;
            private Image image;
            private Aeron aeron;
            private State state;
            private UnsafeBuffer buffer;
            private Publication publication;

            public ServerClient(int session, Image in_image, Aeron in_aeron)
            {
                this.session = session;
                this.image = in_image ?? throw new ArgumentNullException(nameof(in_image));
                this.aeron = in_aeron ?? throw new ArgumentNullException(nameof(in_aeron));
                this.state = State.INITIAL;
                this.buffer = new UnsafeBuffer(BufferUtil.AllocateDirectAligned(2048, 16));
            }

            public void Dispose()
            {
                buffer?.Dispose();
                publication?.Dispose();
            }

            public void OnReceiveMessage(string message)
            {
                Log?.WriteLine($"OnReceiveMessage [0x{this.session}]: {message}");

                switch (state)
                {
                    case State.INITIAL:
                        this.OnReceiveMessageInitial(message);
                        break;
                    case State.CONNECTED:
                        SendMessage(this.publication, this.buffer, message);
                        break;
                }
            }
            private void OnReceiveMessageInitial(string message)
            {
                var matcher = HELLO_PATTERN.Match(message);

                if (!matcher.Success)
                {
                    Log?.WriteLine($"client sent malformed HELLO message: {message}");
                    return;
                }

                string n = matcher.Groups[1].Value;
                int port = int.Parse(n);

                var source_id = image.SourceIdentity;

                try
                {
                    var source_uri = new Uri("fake://" + source_id);

                    var sb = new StringBuilder();
                    sb.Append(source_uri.Host);
                    sb.Append(":");
                    sb.Append(port);

                    var address = sb.ToString();

                    var pub_uri = new ChannelUriStringBuilder()
                        .Reliable(true)
                        .Media("udp")
                        .Endpoint(address)
                        .Build();

                    this.publication = this.aeron.AddPublication(pub_uri, ECHO_STREAM_ID);

                    this.state = State.CONNECTED;
                }
                catch (UriFormatException ex)
                {
                    Log?.WriteLine($"UriFormatException Message: {ex.Message}");
                }
            }
        }

        public static EchoServer Create(DirectoryInfo media_directory, IPEndPoint local_address)
        {
            string directory = media_directory is not null && media_directory.Exists ? media_directory.FullName : throw new DirectoryNotFoundException();

            var aeron_context = new Aeron.Context().AeronDirectoryName(directory);

            Aeron aeron = null;
            try
            {
                aeron = Aeron.Connect(aeron_context);
            }
            catch
            {
                aeron?.Dispose();
                throw;
            }

            return new EchoServer(aeron, local_address);
        }

        public void Run()
        {
            using (Subscription sub = this.SetupSubscription())
            {
                this.RunLoop(sub);
            }
        }

        private void RunLoop(Subscription sub)
        {
            var assembler = new FragmentAssembler(OnParseMessage);

            while (true)
            {
                if (sub.IsConnected)
                {
                    sub.Poll(assembler, 10);
                }
                Thread.Sleep(100);
            }
        }

        private void OnParseMessage(IDirectBuffer buffer, int offset, int length, Header header)
        {
            int session = header.SessionId;

            var client = this.clients[session];

            if (client == null)
            {
                Log?.WriteLine($"received message from unknown client: {session}");
                return;
            }

            var buf = new byte[length];
            buffer.GetBytes(offset, buf);
            var message = Encoding.UTF8.GetString(buf);
            client.OnReceiveMessage(message);
        }
        private static bool SendMessage(Publication pub, UnsafeBuffer buffer, string text)
        {
            Log?.WriteLine($"send: [session 0x{pub.SessionId}] {text}");

            var value = Encoding.UTF8.GetBytes(text);
            buffer.PutBytes(0, value);

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

        private Subscription SetupSubscription()
        {
            var sub_uri = new ChannelUriStringBuilder()
                .Reliable(true)
                .Media("udp")
                .Endpoint(this.local_address.ToString())
                .Build();

            Log?.WriteLine($"subscription URI: {sub_uri}");

            return this.aeron.AddSubscription(sub_uri, ECHO_STREAM_ID, OnClientConnected, OnClientDisconnected);
        }

        private void OnClientDisconnected(Image image)
        {
            int session = image.SessionId;
            Log?.WriteLine($"OnClientDisconnected: {image.SourceIdentity}");

            try
            {
                ServerClient client = this.clients[session];
                this.clients.Remove(session);
                Log?.WriteLine($"OnClientDisconnected: closing client {client}");
            }
            catch (Exception ex)
            {
                Log?.WriteLine($"OnClientDisconnected: failed to close client: {ex.Message}");
            }
        }

        private void OnClientConnected(Image image)
        {
            int session = image.SessionId;

            Log?.WriteLine($"OnClientConnected: {image.SourceIdentity}");

            this.clients.Add(session, new ServerClient(session, image, this.aeron));
        }

        public void Dispose()
        {
            this.aeron?.Dispose();
        }
    }
}