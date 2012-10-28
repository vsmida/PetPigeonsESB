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
        private readonly Dictionary<string, string> _messageTypesToEndpoints = new Dictionary<string, string>();
        private readonly BlockingCollection<ITransportMessage> _messagesToPublish = new BlockingCollection<ITransportMessage>();
        private readonly BlockingCollection<ITransportMessage> _messagesToForward = new BlockingCollection<ITransportMessage>();
        private readonly BlockingCollection<ITransportMessage> _messagesToRaise = new BlockingCollection<ITransportMessage>();
        private readonly BlockingCollection<ITransportMessage> _acknowledgementsToSend = new BlockingCollection<ITransportMessage>();
        public TransportConfiguration Configuration { get; private set; }
        private readonly IZmqSocketManager _socketManager;
        private readonly IQosManager _qosManager;
        public event Action<ITransportMessage> OnMessageReceived = delegate { };
        private volatile bool _running = true;
        private string _serviceIdentity;

        public Transport(TransportConfiguration configuration, IZmqSocketManager socketManager, IQosManager qosManager)
        {
            Configuration = configuration;
            _socketManager = socketManager;
            _qosManager = qosManager;
        }


        public void Initialize(string serviceIdentity)
        {
            _serviceIdentity = serviceIdentity;
            _socketManager.CreateResponseSocket(_messagesToForward, _acknowledgementsToSend, Configuration.GetCommandsEnpoint(), _serviceIdentity);
            _socketManager.CreatePublisherSocket(_messagesToPublish, Configuration.GetEventsEndpoint());
            CreateQosInspectionThread();
            CreateTransportMessageProcessingThread();
        }

        private void CreateQosInspectionThread()
        {
            new BackgroundThread(() =>
            {
                while (_running)
                {
                    ITransportMessage message;
                    if (_messagesToForward.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                    {
                        _qosManager.InspectMessage(message);
                        _messagesToRaise.Add(message);
                    }
                }

            }).Start();
        }

        private void CreateTransportMessageProcessingThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while(_running)
                                         {
                                             ITransportMessage message;
                                             if (_messagesToRaise.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }
                                         
                                     }).Start();
        }

        public void SendMessage(ITransportMessage message, IQosStrategy strategy)
        {
            _qosManager.RegisterMessage(message, strategy);
            _endpointsToMessageQueue[_messageTypesToEndpoints[message.MessageType]].Add(message);
            strategy.WaitForQosAssurancesToBeFulfilled(message);
        }



        public void PublishMessage(ITransportMessage message, IQosStrategy strategy) 
        {
            _qosManager.RegisterMessage(message, strategy);
            _messagesToPublish.Add(message);
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
            _messageTypesToEndpoints[typeof(T).FullName] = endpoint;
            if (!_endpointsToMessageQueue.ContainsKey(endpoint))
            {
                _endpointsToMessageQueue[endpoint] = new BlockingCollection<ITransportMessage>();
                _socketManager.CreateRequestSocket(_endpointsToMessageQueue[endpoint], _messagesToForward, endpoint, _serviceIdentity);
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