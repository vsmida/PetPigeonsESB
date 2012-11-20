using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.Transport
{
    public class SendingStrategyKey
    {
        public Guid MessageId { get; set; }

        public SendingStrategyKey(Guid originalMessageId)
        {
            MessageId = originalMessageId;
        }

        protected bool Equals(SendingStrategyKey other)
        {
            return MessageId.Equals(other.MessageId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SendingStrategyKey)obj);
        }

        public override int GetHashCode()
        {
            return MessageId.GetHashCode();
        }
    }

    public abstract class StartupReliabilityStrategy : IStartupReliabilityStrategy
    {
        public string PeerName { get; private set; }
        public string MessageType { get; private set; }
        public bool IsInitialized { get; protected set; }
        public abstract IEnumerable<IReceivedTransportMessage> GetMessagesToBubbleUp(IReceivedTransportMessage message);

        protected StartupReliabilityStrategy(string peerName, string messageType)
        {
            PeerName = peerName;
            MessageType = messageType;
        }
    }


    internal class SynchronizeWithBrokerStartupStrategy : StartupReliabilityStrategy
    {
        private readonly string _peerName;
        private readonly string _messageType;
        private readonly Queue<IReceivedTransportMessage> _bufferizedMessages = new Queue<IReceivedTransportMessage>();
        private readonly int _bufferSize = 500;
        private readonly IPersistenceSynchronizer _persistenceSynchronizer;

        public SynchronizeWithBrokerStartupStrategy(string peerName, string messageType, IPersistenceSynchronizer persistenceSynchronizer)
            : base(peerName, messageType)
        {
            _peerName = peerName;
            _messageType = messageType;
            _persistenceSynchronizer = persistenceSynchronizer;
        }

        public override IEnumerable<IReceivedTransportMessage> GetMessagesToBubbleUp(IReceivedTransportMessage message)
        {
            if (IsInitialized)
            {
                yield return message;
                yield break;
            }

            var firstElement = _bufferizedMessages.Peek();
            if (firstElement == null || firstElement.MessageIdentity != message.MessageIdentity)
                EnqueueMessage(message);

            else
                foreach (var transportMessage in SetInitialized())
                    yield return transportMessage;

        }

        private IEnumerable<IReceivedTransportMessage> SetInitialized()
        {
            IsInitialized = true;
            foreach (var bufferizedMesage in _bufferizedMessages)
            {
                yield return bufferizedMesage;
            }
        }

        private void EnqueueMessage(IReceivedTransportMessage message)
        {
            if (_bufferSize < _bufferizedMessages.Count)
            {
                _bufferizedMessages.Clear();
            }
            _bufferizedMessages.Enqueue(message);
        }
    }

    internal class FireAndForgetStartupStrategy : StartupReliabilityStrategy
    {
        public FireAndForgetStartupStrategy(string peerName, string messageType)
            : base(peerName, messageType)
        {
        }


        public override IEnumerable<IReceivedTransportMessage> GetMessagesToBubbleUp(IReceivedTransportMessage message)
        {
            yield return message;
        }
    }
}