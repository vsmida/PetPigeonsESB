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
        private readonly Dictionary<string, BlockingCollection<ITransportMessage>> _endpointsToMessageQueue = new Dictionary<string, BlockingCollection<ITransportMessage>>();
        private readonly Dictionary<Type, string> _messageTypesToEndpoints = new Dictionary<Type, string>();
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
            _socketManager.CreateResponseSocket(_messagesToForward,_acknowledgementsToSend, Configuration.GetCommandsEnpoint(), Configuration.Identity);
            _socketManager.CreatePublisherSocket(_messagesToPublish, Configuration.GetEventsEndpoint());
            CreateTransportMessageProcessingThread();
        }

        private void CreateTransportMessageProcessingThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while(_running)
                                         {
                                             ITransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }
                                         
                                     }).Start();
        }

        public void SendMessage<T>(T message) where T : IMessage
        {
            _endpointsToMessageQueue[_messageTypesToEndpoints[typeof(T)]].Add(new TransportMessage(Guid.NewGuid(),Configuration.Identity, typeof(T).FullName, Serializer.Serialize(message)));
        }



        public void PublishMessage<T>(T message) where T : IMessage
        {
            _messagesToPublish.Add(new TransportMessage(Guid.NewGuid(), null,typeof(T).FullName, Serializer.Serialize(message)));
        }

        public void AckMessage(string recipientIdentity, Guid messageId, bool success)
        {
            _acknowledgementsToSend.Add(new TransportMessage(Guid.NewGuid(), recipientIdentity, typeof(AcknowledgementMessage).FullName, Serializer.Serialize(new AcknowledgementMessage(messageId, success))));
        }

        public void RegisterPublisherEndpoint<T>(string endpoint) where T : IMessage
        {
            _socketManager.CreateSubscribeSocket(_messagesToForward, endpoint);
        }

        public void RegisterCommandHandlerEndpoint<T>(string endpoint) where T : IMessage
        {
            _messageTypesToEndpoints[typeof(T)] = endpoint;
            if (!_endpointsToMessageQueue.ContainsKey(endpoint))
            {
                _endpointsToMessageQueue[endpoint] = new BlockingCollection<ITransportMessage>();
                _socketManager.CreateRequestSocket(_endpointsToMessageQueue[endpoint], _messagesToForward, endpoint, Configuration.Identity);
            }
        }


        public void Dispose()
        {
            _running = false;
            _socketManager.Stop();
        }

        private void CompleteAddingCollections()
        {
            _messagesToPublish.CompleteAdding();
            _messagesToForward.CompleteAdding();
            foreach (var pair in _endpointsToMessageQueue)
            {
                pair.Value.CompleteAdding();
            }
        }
    }
}