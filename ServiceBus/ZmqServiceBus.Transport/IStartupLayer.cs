using System;
using System.Collections.Generic;
using PersistenceService.Commands;
using Shared;

namespace ZmqServiceBus.Transport
{
    public interface IStartupLayer : IDisposable
    {
        void Send(ITransportMessage message);
        void Publish(ITransportMessage message);
        void Route(ITransportMessage message);
        void RegisterMessageReliabilitySetting<T>(MessageOptions level);
        event Action<ITransportMessage> OnMessageReceived;
        void Initialize();
    }

    public class StartupLayer : IStartupLayer
    {
        private readonly IReliabilityLayer _reliabilityLayer;
        private readonly Dictionary<StartUpKey, InitializationData> _buffersByKey = new Dictionary<StartUpKey, InitializationData>();
        private readonly HashSet<string> _unsynchronizedMessageTypes = new HashSet<string>();
        private const int _bufferSize = 500;

        public StartupLayer(IReliabilityLayer reliabilityLayer)
        {
            _reliabilityLayer = reliabilityLayer;
            _reliabilityLayer.OnMessageReceived += OnReliabilityLayerMessageReceived;
        }

        private void OnReliabilityLayerMessageReceived(ITransportMessage message)
        {
            var messageKey = new StartUpKey(message.PeerName, message.MessageType);
            if (Initialized(messageKey))
                OnMessageReceived(message);
            else
            {
                if (!IsSynchronizationMessage(message))
                    EnqueueMessage(message);
                else
                {
                    if(IsBufferized(message))
                    {
                        ProcessBuffer(messageKey);
                        MarkAsInitialized(messageKey);
                    }
                }
                
            }
              
        }

        private void MarkAsInitialized(StartUpKey messageKey)
        {
            var initializationData = _buffersByKey[messageKey];
            initializationData.BufferizedMessages.Clear();
            initializationData.IsInitialized = true;
        }

        private void ProcessBuffer(StartUpKey messageKey)
        {
            var queue = _buffersByKey[messageKey].BufferizedMessages;
            foreach (var transportMessage in queue)
            {
                OnMessageReceived(transportMessage);
            }

        }


        private bool IsBufferized(ITransportMessage message)
        {
            var queue = _buffersByKey[new StartUpKey(message.PeerName, message.MessageType)].BufferizedMessages;
            var firstElement = queue.Peek();
            if (firstElement == null || firstElement.MessageIdentity != message.MessageIdentity)
                return false;
            return true;
        }

        private void EnqueueMessage(ITransportMessage message)
        {
            var queue = _buffersByKey[new StartUpKey(message.PeerName, message.MessageType)].BufferizedMessages;
            if(queue.Count == _bufferSize)
                queue.Clear();
            queue.Enqueue(message);
        }

        private bool IsSynchronizationMessage(ITransportMessage message)
        {
            return message.MessageType == typeof (ProcessMessagesCommand).FullName;
        }

        private bool Initialized(StartUpKey messageKey)
        {
            if (_unsynchronizedMessageTypes.Contains(messageKey.MessageType))
                return true;

            InitializationData data = null;
            if (!_buffersByKey.ContainsKey(messageKey))
            {
                data = new InitializationData();
                _buffersByKey.Add(messageKey, data);
            }
            return data.IsInitialized;
        }

        public void Send(ITransportMessage message)
        {
            _reliabilityLayer.Send(message);
        }

        public void Publish(ITransportMessage message)
        {
           _reliabilityLayer.Publish(message);
        }

        public void Route(ITransportMessage message)
        {
            _reliabilityLayer.Route(message);
        }

        public void RegisterMessageReliabilitySetting<T>(MessageOptions level)
        {
            if (level.ReliabilityLevel == ReliabilityLevel.FireAndForget)
                _unsynchronizedMessageTypes.Add(typeof (T).FullName);
        }

        public event Action<ITransportMessage> OnMessageReceived;
        public void Initialize()
        {
            _reliabilityLayer.Initialize();
        }


        private class InitializationData
        {
            public Queue<ITransportMessage> BufferizedMessages { get; set; }
            public bool IsInitialized { get; set; }

            public InitializationData()
            {
                BufferizedMessages = new Queue<ITransportMessage>();
            }
        }


        private class StartUpKey
        {
            public string PeerName { get; set; }
            public string MessageType { get; set; }

            public StartUpKey(string peerName, string messageType)
            {
                PeerName = peerName;
                MessageType = messageType;
            }

            protected bool Equals(StartUpKey other)
            {
                return string.Equals(PeerName, other.PeerName) && string.Equals(MessageType, other.MessageType);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((StartUpKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((PeerName != null ? PeerName.GetHashCode() : 0)*397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
                }
            }
        }

        public void Dispose()
        {
            _reliabilityLayer.Dispose();
        }
    }
}