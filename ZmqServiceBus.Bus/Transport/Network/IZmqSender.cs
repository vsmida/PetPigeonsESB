using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IZmqSender : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingTransportMessage message);
        void PublishMessage(ISendingTransportMessage message);
        void RouteMessage(ISendingTransportMessage message, string destinationPeer);
    }

    public class ZmqSender : IZmqSender
    {

        private enum ActionType
        {
            PublishMessage,
            SendMessage,
            DisconnectSocket
        }

        private class SendableTransportMessage
        {
            public IEnumerable<string> PeersToSendTo;
            public ISendingTransportMessage SendingTransportMessage;

            public SendableTransportMessage(IEnumerable<string> peersToSendTo, ISendingTransportMessage sendingTransportMessage)
            {
                PeersToSendTo = peersToSendTo;
                SendingTransportMessage = sendingTransportMessage;
            }
        }

        private TransportConfiguration _configuration;
        private ZmqContext _context;
        private ZmqSocket _publisherSocket;
        private Dictionary<string, ZmqSocket> _peerNameToDestinationSocket;
        private BlockingCollection<SendableTransportMessage> _messagesToSend;
        private readonly bool _running = false;
        private IPeerManager _peerManager;


        public ZmqSender(ZmqContext context, TransportConfiguration configuration, IPeerManager peerManager)
        {
            _context = context;
            _configuration = configuration;
            _peerManager = peerManager;
            _peerManager.PeerConnected += OnPeerConnected;
        }

        private void OnPeerConnected(IServicePeer peer)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            CreatePublisherSocket();

            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             foreach ( var messageToSend in _messagesToSend.GetConsumingEnumerable())
                                             {
                                                 if(messageToSend.PeersToSendTo == null)
                                                 {
                                                     SendOnPubSocket(messageToSend.SendingTransportMessage, _publisherSocket);
                                                     continue;
                                                 }

                                                 foreach (var peer in messageToSend.PeersToSendTo)
                                                 {
                                                     ZmqSocket socket;
                                                     if(!_peerNameToDestinationSocket.TryGetValue(peer,out socket))
                                                     {
                                                        socket = CreateSocketForPeer(messageToSend, peer);
                                                     }
                                                     SendOnPubSocket(messageToSend.SendingTransportMessage, socket);
                                                 }
                                             }
                                         }
                                     });
        }

        private ZmqSocket CreateSocketForPeer(SendableTransportMessage messageToSend, string peer)
        {
            ZmqSocket socket;
            var endpointToConnectTo = _peerManager.GetPeerEndpointFor(messageToSend.SendingTransportMessage.MessageType, peer);
            socket = _context.CreateSocket(SocketType.PUB);
            socket.Linger = TimeSpan.FromMilliseconds(500);
            socket.SendHighWatermark = 10000;
            socket.Connect(endpointToConnectTo);
            _peerNameToDestinationSocket.Add(peer, socket);
            return socket;
        }

        private void SendOnPubSocket(ISendingTransportMessage sendingTransportMessage, ZmqSocket socket)
        {
            throw new NotImplementedException();
        }


        private void CreatePublisherSocket()
        {
            _publisherSocket = _context.CreateSocket(SocketType.PUB);
            _publisherSocket.Linger = TimeSpan.FromMilliseconds(500);
            _publisherSocket.SendHighWatermark = 20000;
            _publisherSocket.Bind(_configuration.GetEventsBindEndpoint());
        }

        public void SendMessage(ISendingTransportMessage message)
        {
            
        }

        public void PublishMessage(ISendingTransportMessage message)
        {
            throw new System.NotImplementedException();
        }

        public void RouteMessage(ISendingTransportMessage message, string destinationPeer)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}