﻿using System;
using System.Collections.Generic;
using System.Threading;
using ZeroMQ;
using Bus.Transport;

namespace Tests.Integration
{
    public class IntegrationTestsMockCreator
    {
        private volatile bool _running = true;
        private Thread _directoryServiceThread;

        public void StopDirectoryService()
        {
            _running = false;
            _directoryServiceThread.Join();
        }

        public void CreateFakeDirectoryService(int port)
        {
            _directoryServiceThread = new Thread(() =>
                                                     {
                                                         var peerList = new List<ServicePeer>();
                                                         var peerSockets = new Dictionary<string, ZmqSocket>();
                                                         var context = ZmqContext.Create();
                                                         var receptionSocket = context.CreateSocket(SocketType.PULL);
                                                         receptionSocket.Linger = TimeSpan.Zero;
                                                         receptionSocket.Bind("tcp://*:"+port);
                                                 //        receptionSocket.ReceiveReady += (s, e) => OnFakeDirectoryServiceReceptionRouterReceive(s, e, context, peerSockets, peerList);

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


                                                     }) {IsBackground = false};
            _directoryServiceThread.Start();
        }


        //private void OnFakeDirectoryServiceReceptionRouterReceive(object sender, SocketEventArgs e, ZmqContext context, Dictionary<string, ZmqSocket> spawnedSockets, List<ServicePeer> peerList)
        //{
        //    var zmqSocket = sender as ZmqSocket;
        //    var type = zmqSocket.Receive(Encoding.ASCII);
        //    var peerName = zmqSocket.Receive(Encoding.ASCII);
        //    var serializedId = zmqSocket.Receive();
        //    var messageId = new Guid(serializedId);
        //    var serializedItem = zmqSocket.Receive();


        //    if (type == typeof(RegisterPeerCommand).FullName)
        //    {
        //        var command = BusSerializer.Deserialize<RegisterPeerCommand>(serializedItem);
        //        ServicePeer peerToAdd = command.Peer;
        //        if(command.Peer.PeerName == "Service1")
        //        {
        //            peerToAdd = new ServicePeer(command.Peer.PeerName,
        //                                                     command.Peer.HandledMessages.Where(x => x.MessageType != typeof(FakeCommand)).Cast<MessageSubscription>());
        //        }
        //        peerList.Add(peerToAdd);
        //        var initCommand = new InitializeTopologyAndMessageSettings(peerList,
        //                                                                   new List<MessageOptions>
        //                                                                       {
        //                                                                           new MessageOptions(
        //                                                                               typeof (FakeCommand).FullName,
        //                                                                               ReliabilityLevel.FireAndForget)
        //                                                                       });
        //        ZmqSocket sendingSocket;
        //        if(!spawnedSockets.TryGetValue(command.Peer.PeerName, out sendingSocket))
        //        {
        //            sendingSocket = context.CreateSocket(SocketType.PUSH);
        //            sendingSocket.Linger = TimeSpan.FromMilliseconds(200);
        //            var endpoint = command.Peer.HandledMessages.First().Endpoint as ZmqEndpoint;
        //            sendingSocket.Connect(endpoint.Endpoint);
        //            spawnedSockets[command.Peer.PeerName] = sendingSocket;
        //        }

        //        sendingSocket.SendMore(Encoding.ASCII.GetBytes(typeof(InitializeTopologyAndMessageSettings).FullName));
        //        sendingSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
        //        sendingSocket.SendMore(Guid.NewGuid().ToByteArray());
        //        sendingSocket.Send(BusSerializer.Serialize(initCommand));

        //        var peerConnectedEvent = new PeerConnected(command.Peer);
        //        foreach (var socket in spawnedSockets.Values)
        //        {
        //            socket.SendMore(Encoding.ASCII.GetBytes(typeof(PeerConnected).FullName));
        //            socket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
        //            socket.SendMore(Guid.NewGuid().ToByteArray());
        //            socket.Send(BusSerializer.Serialize(peerConnectedEvent));
        //        }
                
        //        SendCompletionMessage(messageId, sendingSocket);
        //    }


        //}

        //private static void SendCompletionMessage(Guid messageId, ZmqSocket sendingSocket)
        //{
        //    var completionMess = new CompletionAcknowledgementMessage(messageId, true);
        //    sendingSocket.SendMore(Encoding.ASCII.GetBytes(typeof (CompletionAcknowledgementMessage).FullName));
        //    sendingSocket.SendMore(Encoding.ASCII.GetBytes("DirectoryService"));
        //    sendingSocket.SendMore(Guid.NewGuid().ToByteArray());
        //    sendingSocket.Send(BusSerializer.Serialize(completionMess));
        //}


    }
}