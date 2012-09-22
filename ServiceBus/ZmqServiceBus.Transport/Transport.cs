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
        private readonly ITransportConfiguration _config;
        private readonly IZmqSocketManager _socketManager;
        public event Action<ITransportMessage> OnMessageReceived = delegate { };
        private volatile bool _running = true;

        public Transport(ITransportConfiguration config, IZmqSocketManager socketManager)
        {
            _config = config;
            _socketManager = socketManager;
        }


        public void Initialize()
        {
            _socketManager.CreateResponseSocket(_messagesToForward,_acknowledgementsToSend, GetCommandReplierEndpoint(), _config.Identity);
            _socketManager.CreatePublisherSocket(_messagesToPublish, GetPublisherSocketEndpoint());
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
            _endpointsToMessageQueue[_messageTypesToEndpoints[typeof(T)]].Add(new TransportMessage(_config.Identity, typeof(T).FullName, Serializer.Serialize(message)));
        }

        private string GetCommandReplierEndpoint()
        {
            return _config.CommandsProtocol + "://*:" + _config.CommandsPort;
        }

        private string GetPublisherSocketEndpoint()
        {
            return _config.EventsProtocol + "://*:" + _config.EventsPort;
        }

        public void PublishMessage<T>(T message) where T : IMessage
        {
            _messagesToPublish.Add(new TransportMessage(null,typeof(T).FullName, Serializer.Serialize(message)));
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
                _socketManager.CreateRequestSocket(_endpointsToMessageQueue[endpoint], _messagesToForward, endpoint, _config.Identity);
            }
        }


        public void Dispose()
        {
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