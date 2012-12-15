using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IDataSender : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingBusMessage message);
        void PublishMessage(ISendingBusMessage message);
        void RouteMessage(ISendingBusMessage message, string destinationPeer);
    }

    public class DataSender : IDataSender
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
            public ISendingBusMessage SendingBusMessage;

            public SendableTransportMessage(IEnumerable<string> peersToSendTo, ISendingBusMessage sendingBusMessage)
            {
                PeersToSendTo = peersToSendTo;
                SendingBusMessage = sendingBusMessage;
            }
        }

        private TransportConfiguration _configuration;
        private ZmqContext _context;
        private ZmqSocket _publisherSocket;
        private Dictionary<string, ZmqSocket> _peerNameToDestinationSocket;
        private BlockingCollection<SendableTransportMessage> _messagesToSend;
        private readonly bool _running = false;
        private IPeerManager _peerManager;


        public DataSender(ZmqContext context, TransportConfiguration configuration, IPeerManager peerManager)
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
                                         foreach (var messageToSend in _messagesToSend.GetConsumingEnumerable())
                                         {
                                             if (messageToSend.PeersToSendTo == null)
                                             {
                                                 SendOnPubSocket(messageToSend.SendingBusMessage, _publisherSocket);
                                                 continue;
                                             }

                                             foreach (var peer in messageToSend.PeersToSendTo)
                                             {
                                                 ZmqSocket socket;
                                                 if (!_peerNameToDestinationSocket.TryGetValue(peer, out socket))
                                                 {
                                                     socket =
                                                         CreateSocketForPeer(
                                                             messageToSend.SendingBusMessage.MessageType, peer);
                                                 }
                                                 SendOnPubSocket(messageToSend.SendingBusMessage, socket);
                                             }
                                         }
                                     }
                                 });
        }

        private ZmqSocket CreateSocketForPeer(string messageType, string peer)
        {
            ZmqSocket socket;
            var endpointToConnectTo = _peerManager.GetPeerEndpointFor(messageType, peer);
            socket = _context.CreateSocket(SocketType.PUB);
            socket.Linger = TimeSpan.FromMilliseconds(500);
            socket.SendHighWatermark = 10000;
            socket.Connect(endpointToConnectTo);
            _peerNameToDestinationSocket.Add(peer, socket);
            return socket;
        }

        private void SendOnPubSocket(ISendingBusMessage sendingBusMessage, ZmqSocket socket)
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

        public void SendMessage(ISendingBusMessage message)
        {
            
        }

        public void PublishMessage(ISendingBusMessage message)
        {
            throw new System.NotImplementedException();
        }

        public void RouteMessage(ISendingBusMessage message, string destinationPeer)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}