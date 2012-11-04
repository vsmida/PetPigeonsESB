using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using ProtoBuf.Meta;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public class Transport : ITransport
    {
        private class SocketInfo
        {
            public BlockingCollection<ITransportMessage> SendingQueue { get; set; }
            public bool SocketInitialized { get; set; }

            public SocketInfo()
            {
                SendingQueue = new BlockingCollection<ITransportMessage>();
            }
        }

        private readonly Dictionary<string, SocketInfo> _endpointsToSocketInfo = new Dictionary<string, SocketInfo>();
        private readonly Dictionary<string, string> _messageTypesToEndpoints = new Dictionary<string, string>();
        private readonly BlockingCollection<ITransportMessage> _messagesToPublish = new BlockingCollection<ITransportMessage>();
        private readonly BlockingCollection<ITransportMessage> _messagesToForward = new BlockingCollection<ITransportMessage>();
        private readonly BlockingCollection<ITransportMessage> _acknowledgementsToSend = new BlockingCollection<ITransportMessage>();
        public TransportConfiguration Configuration { get; private set; }
        private readonly IZmqSocketManager _socketManager;
        public event Action<ITransportMessage> OnMessageReceived = delegate { };
        private volatile bool _running = true;

        public Transport(TransportConfiguration configuration, IZmqSocketManager socketManager)
        {
            Configuration = configuration;
            _socketManager = socketManager;
        }


        public void Initialize()
        {
            _socketManager.CreateResponseSocket(_messagesToForward, Configuration.GetCommandsEnpoint(), Configuration.PeerName);
            _socketManager.CreatePublisherSocket(_messagesToPublish, Configuration.GetEventsEndpoint(), Configuration.PeerName);
            _socketManager.CreateSubscribeSocket(_messagesToForward);
            CreateTransportMessageProcessingThread();
        }

        private void CreateTransportMessageProcessingThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             ITransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }

                                     }).Start();
        }

        public void SendMessage(ITransportMessage message)
        {
            var endpoint = _messageTypesToEndpoints[message.MessageType];
            var socketInfo = _endpointsToSocketInfo[endpoint];
            if(!socketInfo.SocketInitialized)
            {
                _socketManager.CreateRequestSocket(socketInfo.SendingQueue, _messagesToForward, endpoint, Configuration.PeerName);
                socketInfo.SocketInitialized = true;
            }
            socketInfo.SendingQueue.Add(message);
        }



        public void PublishMessage(ITransportMessage message)
        {
            _messagesToPublish.Add(message);
        }

        public void AckMessage(byte[] recipientIdentity, Guid messageId, bool success)
        {
         //   _acknowledgementsToSend.Add(new TransportMessage(Guid.NewGuid(), typeof(AcknowledgementMessage).FullName, Serializer.Serialize(new AcknowledgementMessage(messageId, success))));
        }

        public void RegisterPeer(IServicePeer peer)
        {
            foreach (var publishedMessageType in peer.PublishedMessages)
            {
                _socketManager.SubscribeTo(peer.PublicationEndpoint, publishedMessageType.FullName);
            }

            foreach (var handledMessage in peer.HandledMessages)
            {
                _messageTypesToEndpoints[handledMessage.FullName] = peer.ReceptionEndpoint;
                if (!_endpointsToSocketInfo.ContainsKey(peer.ReceptionEndpoint))
                {
                    _endpointsToSocketInfo[peer.ReceptionEndpoint] = new SocketInfo();
                }
            }
        }


        public void Dispose()
        {
            _running = false;
            _socketManager.Stop();
        }

    }
}