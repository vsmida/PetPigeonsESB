using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Tests.Integration
{
    public class TestBusCreator : MarshalByRefObject
    {
        private List<ServicePeer> _peers = new List<ServicePeer>();
        private Dictionary<string, ZmqSocket> _peerToZmqSocket = new Dictionary<string, ZmqSocket>(); 
        private volatile bool _running = true;

        public IBus GetBus(string peerName)
        {
            StructureMap.ObjectFactory.Initialize(x => x.AddRegistry<BusRegistry>());
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            StructureMap.ObjectFactory.Configure(x => x.For<ZmqTransportConfiguration>()
                                                           .Use(new DummyTransportConfig(randomPort1,peerName)));
            StructureMap.ObjectFactory.Configure(x => x.For<IBusBootstrapperConfiguration>().Use(new DummyBootstrapperConfig
            {
                DirectoryServiceEndpoint = "tcp://localhost:111",
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
                                          var receptionSocket = context.CreateSocket(SocketType.PULL);
                                          receptionSocket.Linger = TimeSpan.Zero;
                                          receptionSocket.Bind("tcp://*:111");
                                          receptionSocket.ReceiveReady += (s,e) =>OnReceptionRouterReceive(s,e,context);


//                                          _pushSocket1 = context.CreateSocket(SocketType.PUSH);
  //                                        _pushSocket1.Linger = TimeSpan.Zero;
    //                                      _pushSocket1.Bind("tcp://*:222");

                                          Poller poller = new Poller();
                                          poller.AddSocket(receptionSocket);
                                          while(_running)
                                          {
                                              poller.Poll(TimeSpan.FromMilliseconds(50));
                                              
                                          }
                                          foreach (var zmqSocket in _peerToZmqSocket.Values)
                                          {
                                              zmqSocket.Dispose();
                                          }
                                          receptionSocket.Dispose();
                                          context.Dispose();


                                      }) {IsBackground = false}.Start();
        }



        private void OnReceptionRouterReceive(object sender, SocketEventArgs e, ZmqContext context)
        {
            var zmqSocket = sender as ZmqSocket;
            var type = zmqSocket.Receive(Encoding.ASCII);
            var peerName = zmqSocket.Receive(Encoding.ASCII);
            var serializedId = zmqSocket.Receive();
            var messageId = new Guid(serializedId);
            var serializedItem = zmqSocket.Receive();

            if (type == typeof(ReceivedOnTransportAcknowledgement).FullName)
                return;


            if (type == typeof(RegisterPeerCommand).FullName)
            {
                var command = Serializer.Deserialize<RegisterPeerCommand>(serializedItem);
                ServicePeer peerToAdd = command.Peer;
                if(command.Peer.PeerName == "Service1")
                {
                    peerToAdd = new ServicePeer(command.Peer.PeerName,
                                                             command.Peer.HandledMessages.Where(x => x.MessageType != typeof(FakeCommand)).Cast<MessageSubscription>());
                }
                _peers.Add(peerToAdd);
                var initCommand = new InitializeTopologyAndMessageSettings(_peers,
                                                                           new List<MessageOptions>
                                                                               {
                                                                                   new MessageOptions(
                                                                                       typeof (FakeCommand).FullName,
                                                                                       new ReliabilityInfo(
                                                                                           ReliabilityLevel.
                                                                                               FireAndForget))
                                                                               });
                ZmqSocket sendingSocket;
                if(!_peerToZmqSocket.TryGetValue(command.Peer.PeerName, out sendingSocket))
                {
                    sendingSocket = context.CreateSocket(SocketType.PUSH);
                    sendingSocket.Linger = TimeSpan.FromMilliseconds(200);
                    var endpoint = command.Peer.HandledMessages.First().Endpoint as ZmqEndpoint;
                    sendingSocket.Connect(endpoint.Endpoint);
                    _peerToZmqSocket[command.Peer.PeerName] = sendingSocket;
                }

                sendingSocket.SendMore(Encoding.ASCII.GetBytes(typeof(InitializeTopologyAndMessageSettings).FullName));
                sendingSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                sendingSocket.SendMore(Guid.NewGuid().ToByteArray());
                sendingSocket.Send(Serializer.Serialize(initCommand));

                var peerConnectedEvent = new PeerConnected(command.Peer);
                foreach (var socket in _peerToZmqSocket.Values)
                {
                    socket.SendMore(Encoding.ASCII.GetBytes(typeof(PeerConnected).FullName));
                    socket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                    socket.SendMore(Guid.NewGuid().ToByteArray());
                    socket.Send(Serializer.Serialize(peerConnectedEvent));
                }
                

                var completionMess = new CompletionAcknowledgementMessage(messageId, true);
                sendingSocket.SendMore(Encoding.ASCII.GetBytes(typeof(CompletionAcknowledgementMessage).FullName));
                sendingSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                sendingSocket.SendMore(Guid.NewGuid().ToByteArray());
                sendingSocket.Send(Serializer.Serialize(completionMess));

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