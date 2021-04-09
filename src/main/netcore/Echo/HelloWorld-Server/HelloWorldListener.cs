using Adaptive.Aeron;
using Adaptive.Aeron.LogBuffer;
using Adaptive.Agrona;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace HelloWorld_Server
{
    public class HelloWorldListener : IDisposable
    {
        public static TextWriter Log = null;

        private static readonly int HELLOWORLD_STREAM_ID = 0x2044f002;

        private readonly Aeron aeron;
        private readonly IPEndPoint local_address;
        private readonly Dictionary<int, ServerClient> clients;

        private HelloWorldListener(Aeron in_aeron, IPEndPoint in_local_address)
        {
            this.aeron = in_aeron ?? throw new ArgumentNullException(nameof(in_aeron));
            this.local_address = in_local_address ?? throw new ArgumentNullException(nameof(in_local_address));

            this.clients = new Dictionary<int, ServerClient>(32);
        }

        private class ServerClient
        {
            private enum State
            {
                INITIAL,
                CONNECTED
            }

            private int session;
            private State state;

            public ServerClient(int session)
            {
                this.session = session;
                this.state = State.INITIAL;
            }

            public void OnReceiveMessage(string message)
            {
                switch (state)
                {
                    case State.INITIAL:
                        this.OnReceiveMessageInitial(message);
                        break;
                    case State.CONNECTED:
                        this.OnReceiveMessageConnected(message);
                        break;
                }
            }

            private void OnReceiveMessageInitial(string message)
            {
                Log?.WriteLine($"OnReceiveMessage Initial: Session: {session}, Message: {message}");
            }

            private void OnReceiveMessageConnected(string message)
            {
                Log?.WriteLine($"OnReceiveMessage Connected: Session: {session}, Message: {message}");
            }
        }

        public static HelloWorldListener Create(DirectoryInfo media_directory, IPEndPoint local_address)
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

            return new HelloWorldListener(aeron, local_address);
        }

        public void Run()
        {
            using (var sub = this.SetupSubscription())
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
        private Subscription SetupSubscription()
        {
            var sub_uri = new ChannelUriStringBuilder()
                .Reliable(true)
                .Media("udp")
                .Endpoint(this.local_address.ToString())
                .Build();

            Log?.WriteLine($"subscription URI: {sub_uri}");

            return this.aeron.AddSubscription(sub_uri, HELLOWORLD_STREAM_ID, OnClientConnected, OnClientDisconnected);
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

            this.clients.Add(session, new ServerClient(session));
        }

        public void Dispose()
        {
            this.aeron?.Dispose();
        }
    }
}