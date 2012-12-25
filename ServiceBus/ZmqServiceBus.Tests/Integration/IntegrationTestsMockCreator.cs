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
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests.Integration
{
    public class IntegrationTestsMockCreator
    {
        private volatile bool _running = true;

        public void StopDirectoryService()
        {
            _running = false;
        }

        public void CreateFakeDirectoryService(int port)
        {
            new Thread(() =>
                                      {
                                          var peerList = new List<ServicePeer>();
                                          var peerSockets = new Dictionary<string, ZmqSocket>();
                                          var context = ZmqContext.Create();
                                          var receptionSocket = context.CreateSocket(SocketType.PULL);
                                          receptionSocket.Linger = TimeSpan.Zero;
                                          receptionSocket.Bind("tcp://*:"+port);
                                          receptionSocket.ReceiveReady += (s, e) => OnFakeDirectoryServiceReceptionRouterReceive(s, e, context, peerSockets, peerList);

                                          var poller = new Poller();
                                          poller.AddSocket(receptionSocket);
                                          while(_running)
                                          {
                                              poller.Poll(TimeSpan.FromMilliseconds(50));
                                              
                                          }
                                          foreach (var zmqSocket in peerSockets.Values)
                                          {
                                              zmqSocket.Dispose();
                                          }
                                          receptionSocket.Dispose();
                                          poller.Dispose();
                                          context.Dispose();


                                      }) {IsBackground = false}.Start();
        }



        private void OnFakeDirectoryServiceReceptionRouterReceive(object sender, SocketEventArgs e, ZmqContext context, Dictionary<string, ZmqSocket> spawnedSockets, List<ServicePeer> peerList)
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
                peerList.Add(peerToAdd);
                var initCommand = new InitializeTopologyAndMessageSettings(peerList,
                                                                           new List<MessageOptions>
                                                                               {
                                                                                   new MessageOptions(
                                                                                       typeof (FakeCommand).FullName,
                                                                                       new ReliabilityInfo(
                                                                                           ReliabilityLevel.
                                                                                               FireAndForget))
                                                                               });
                ZmqSocket sendingSocket;
                if(!spawnedSockets.TryGetValue(command.Peer.PeerName, out sendingSocket))
                {
                    sendingSocket = context.CreateSocket(SocketType.PUSH);
                    sendingSocket.Linger = TimeSpan.FromMilliseconds(200);
                    var endpoint = command.Peer.HandledMessages.First().Endpoint as ZmqEndpoint;
                    sendingSocket.Connect(endpoint.Endpoint);
                    spawnedSockets[command.Peer.PeerName] = sendingSocket;
                }

                sendingSocket.SendMore(Encoding.ASCII.GetBytes(typeof(InitializeTopologyAndMessageSettings).FullName));
                sendingSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                sendingSocket.SendMore(Guid.NewGuid().ToByteArray());
                sendingSocket.Send(Serializer.Serialize(initCommand));

                var peerConnectedEvent = new PeerConnected(command.Peer);
                foreach (var socket in spawnedSockets.Values)
                {
                    socket.SendMore(Encoding.ASCII.GetBytes(typeof(PeerConnected).FullName));
                    socket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
                    socket.SendMore(Guid.NewGuid().ToByteArray());
                    socket.Send(Serializer.Serialize(peerConnectedEvent));
                }
                
                SendCompletionMessage(messageId, sendingSocket);
            }


        }

        private static void SendCompletionMessage(Guid messageId, ZmqSocket sendingSocket)
        {
            var completionMess = new CompletionAcknowledgementMessage(messageId, true);
            sendingSocket.SendMore(Encoding.ASCII.GetBytes(typeof (CompletionAcknowledgementMessage).FullName));
            sendingSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
            sendingSocket.SendMore(Guid.NewGuid().ToByteArray());
            sendingSocket.Send(Serializer.Serialize(completionMess));
        }


    }
}