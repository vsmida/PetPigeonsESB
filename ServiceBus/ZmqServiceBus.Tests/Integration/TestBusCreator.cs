using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Tests.Integration
{
    public class TestBusCreator : MarshalByRefObject
    {
        private List<ServicePeer> _peers = new List<ServicePeer>();
        private ZmqSocket _pubSocket;
        private volatile bool _running = true;

        public IBus GetBus(string peerName)
        {
            StructureMap.ObjectFactory.Initialize(x => x.AddRegistry<BusRegistry>());
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            StructureMap.ObjectFactory.Configure(x => x.For<TransportConfiguration>()
                                                           .Use(new DummyTransportConfig(randomPort1, randomPort2,
                                                                                         peerName)));
            StructureMap.ObjectFactory.Configure(x => x.For<IBusBootstrapperConfiguration>().Use(new DummyBootstrapperConfig
            {
                DirectoryServiceCommandEndpoint = "tcp://localhost:74",
                DirectoryServiceEventEndpoint = "tcp://localhost:75",
                DirectoryServiceName = "DirectoryService"
            }));
            return StructureMap.ObjectFactory.GetInstance<IBus>();
        }

        public void StopDirectoryService()
        {
            _running = false;
        }

        public void CreateFakeDirectoryService()
        {
            new Thread(() =>
                                      {
                                          var context = ZmqContext.Create();
                                          var receptionRouter = context.CreateSocket(SocketType.ROUTER);
                                          receptionRouter.Linger = TimeSpan.Zero;
                                          receptionRouter.Bind("tcp://*:74");
                                          receptionRouter.ReceiveReady += OnReceptionRouterReceive;

                                          _pubSocket = context.CreateSocket(SocketType.PUB);
                                          _pubSocket.Linger = TimeSpan.Zero;
                                          _pubSocket.Bind("tcp://*:75");

                                          Poller poller = new Poller();
                                          poller.AddSocket(receptionRouter);
                                          while(_running)
                                          {
                                              poller.Poll(TimeSpan.FromMilliseconds(50));
                                              
                                          }
                                          receptionRouter.Dispose();
                                          _pubSocket.Dispose();
                                          context.Dispose();


                                      }) {IsBackground = false}.Start();
        }

        private void OnReceptionRouterReceive(object sender, SocketEventArgs e)
        {
            var zmqSocket = sender as ZmqSocket;
            var zmqIdentity = zmqSocket.Receive();
            var type = zmqSocket.Receive(Encoding.ASCII);
            var peerName = zmqSocket.Receive(Encoding.ASCII);
            var serializedId = zmqSocket.Receive();
            var messageId = new Guid(serializedId);
            var serializedItem = zmqSocket.Receive();

            if (type == typeof(ReceivedOnTransportAcknowledgement).FullName)
                return;

            Ack(zmqSocket, zmqIdentity, messageId);

            if (type == typeof(RegisterPeerCommand).FullName)
            {
                var command = Serializer.Deserialize<RegisterPeerCommand>(serializedItem);
                _peers.Add(command.Peer);
                var initCommand = new InitializeTopologyAndMessageSettings(_peers,
                                                                           new List<MessageOptions>
                                                                               {
                                                                                   new MessageOptions(
                                                                                       typeof (FakeCommand).FullName,
                                                                                       new ReliabilityInfo(
                                                                                           ReliabilityLevel.
                                                                                               FireAndForget))
                                                                               });
                zmqSocket.SendMore(zmqIdentity);
                zmqSocket.SendMore(new byte[0]);
                zmqSocket.SendMore(Encoding.ASCII.GetBytes(typeof(InitializeTopologyAndMessageSettings).FullName));
                zmqSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                zmqSocket.SendMore(Guid.NewGuid().ToByteArray());
                zmqSocket.Send(Serializer.Serialize(initCommand));

                var peerConnectedEvent = new PeerConnected(command.Peer);
                _pubSocket.SendMore(Encoding.ASCII.GetBytes(typeof (PeerConnected).FullName));
                _pubSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                _pubSocket.SendMore(Guid.NewGuid().ToByteArray());
                _pubSocket.Send(Serializer.Serialize(peerConnectedEvent));
            }
        }

        private static void Ack(ZmqSocket zmqSocket, byte[] zmqIdentity, Guid messageId)
        {
            zmqSocket.SendMore(zmqIdentity);
            zmqSocket.SendMore(new byte[0]);
            zmqSocket.SendMore(Encoding.ASCII.GetBytes(typeof(ReceivedOnTransportAcknowledgement).FullName));
            zmqSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
            zmqSocket.SendMore(messageId.ToByteArray());
            zmqSocket.Send(new byte[0]);
        }
    }
}