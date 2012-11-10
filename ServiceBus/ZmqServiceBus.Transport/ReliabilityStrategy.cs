using System;
using System.Collections.Generic;
using System.Threading;
using PersistenceService.Commands;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Transport
{
    public interface ISendingReliabilityStrategy
    {
        void SendOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message);
        void PublishOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message);
        void RouteOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message);
        void CheckMessage(ITransportMessage message);
    }

    public interface IStartupReliabilityStrategy
    {
        string PeerName { get; }
        string MessageType { get; }
        bool IsInitialized { get; }
        IEnumerable<ITransportMessage> GetMessagesToBubbleUp(ITransportMessage message); //enqueue or release messages when broker is sending same message as client.
    }

    
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

    public interface IReliabilityStrategyFactory
    {
        ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions);
        IStartupReliabilityStrategy GetStartupStrategy(MessageOptions messageOptions, string peerName, string messageType);
    }

    public class ReliabilityStrategyFactory : IReliabilityStrategyFactory
    {
        public ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptionses)
        {
            switch (messageOptionses.ReliabilityLevel)
            {
                case ReliabilityLevel.FireAndForget:
                    return new FireAndForget();
                    break;
                //case ReliabilityOption.SendToClientAndBrokerNoAck:
                //    break;
                case ReliabilityLevel.SomeoneReceivedMessageOnTransport:
                    return new WaitForClientOrBrokerTransportAck(messageOptionses.BrokerName);
                    break;
                //case ReliabilityOption.ClientAndBrokerReceivedOnTransport:
                //    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }

        public IStartupReliabilityStrategy GetStartupStrategy(MessageOptions messageOptions, string peerName, string messageType)
        {
            switch (messageOptions.ReliabilityLevel)
            {
                case ReliabilityLevel.FireAndForget:
                    return new FireAndForgetStartupStrategy(null, null);
                case ReliabilityLevel.SendToClientAndBrokerNoAck:
                case ReliabilityLevel.SomeoneReceivedMessageOnTransport:
                case ReliabilityLevel.ClientAndBrokerReceivedOnTransport:
                    return new SynchronizeWithBrokerStartupStrategy(peerName, messageType);
                default:
                    throw new ArgumentOutOfRangeException("messageOptions");
            }
        }
    }

    public abstract class StartupReliabilityStrategy : IStartupReliabilityStrategy
    {
        public string PeerName { get; private set; }
        public string MessageType { get; private set; }
        public bool IsInitialized { get; protected set; }
        public abstract IEnumerable<ITransportMessage> GetMessagesToBubbleUp(ITransportMessage message);

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
        private readonly Queue<ITransportMessage> _bufferizedMessages = new Queue<ITransportMessage>();
        private readonly int _bufferSize = 500;

        public SynchronizeWithBrokerStartupStrategy(string peerName, string messageType)
            : base(peerName, messageType)
        {
            _peerName = peerName;
            _messageType = messageType;
        }

        public override IEnumerable<ITransportMessage> GetMessagesToBubbleUp(ITransportMessage message)
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

        private IEnumerable<ITransportMessage> SetInitialized()
        {
            IsInitialized = true;
            foreach (var bufferizedMesage in _bufferizedMessages)
            {
                yield return bufferizedMesage;
            }
        }

        private void EnqueueMessage(ITransportMessage message)
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


        public override IEnumerable<ITransportMessage> GetMessagesToBubbleUp(ITransportMessage message)
        {
            yield return message;
        }
    }



    public abstract class SendingReliabilityStrategy : ISendingReliabilityStrategy
    {

        private bool _clientTransportAckReceived;
        private bool _brokerTransportAckReceived;
        private bool _clientDispatchAckReceived;
        private bool _clientDispatchSuccessful;
        protected readonly AutoResetEvent _waitForReliabilityConditionsToBeFulfilled = new AutoResetEvent(false);

        protected SendingReliabilityStrategy()
        {

        }

        public bool ClientTransportAckReceived
        {
            get { return _clientTransportAckReceived; }
            set
            {
                _clientTransportAckReceived = value;
                ReleaseWhenReliabilityAchieved();
            }
        }

        public bool BrokerTransportAckReceived
        {
            get { return _brokerTransportAckReceived; }
            set
            {
                _brokerTransportAckReceived = value;
                ReleaseWhenReliabilityAchieved();
            }
        }

        public bool ClientDispatchAckReceived
        {
            get { return _clientDispatchAckReceived; }
            set
            {
                _clientDispatchAckReceived = value;
                ReleaseWhenReliabilityAchieved();
            }
        }

        public bool ClientDispatchSuccessful
        {
            get { return _clientDispatchSuccessful; }
            set
            {
                _clientDispatchSuccessful = value;
                ReleaseWhenReliabilityAchieved();
            }
        }

        public abstract void SendOn(IEndpointManager endpointManager,ISendingStrategyManager strategyManager, ITransportMessage message);
        public abstract void PublishOn(IEndpointManager endpointManager,ISendingStrategyManager strategyManager, ITransportMessage message);
        public abstract void RouteOn(IEndpointManager endpointManager,ISendingStrategyManager strategyManager, ITransportMessage message);
        public abstract void CheckMessage(ITransportMessage message);


        protected abstract void ReleaseWhenReliabilityAchieved();

    }

    internal class WaitForClientOrBrokerTransportAck : SendingReliabilityStrategy
    {
        private readonly string _brokerPeerName;

        public WaitForClientOrBrokerTransportAck(string brokerPeerName)
        {
            _brokerPeerName = brokerPeerName;
        }

        public override void SendOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message)
        {
            SendReliabilityMessage(endpointManager, strategyManager, message);
            endpointManager.SendMessage(message);
        }

        private void SendReliabilityMessage(IEndpointManager endpointManager, ISendingStrategyManager strategyManager,
                                            ITransportMessage message)
        {
            var brokerMessage = new TransportMessage(typeof (PersistMessageCommand).FullName, _brokerPeerName,
                                                     message.MessageIdentity, Serializer.Serialize(message));
            strategyManager.RegisterMessageId(message.MessageIdentity, this);
            strategyManager.RegisterMessageId(brokerMessage.MessageIdentity, this);
            endpointManager.RouteMessage(brokerMessage);
        }

        public override void PublishOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message)
        {
            SendReliabilityMessage(endpointManager, strategyManager, message);
            endpointManager.PublishMessage(message);
        }

        public override void RouteOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message)
        {
            SendReliabilityMessage(endpointManager, strategyManager, message);
            endpointManager.RouteMessage(message);
        }

        public override void CheckMessage(ITransportMessage message)
        {
            if (message.PeerName == _brokerPeerName)
                BrokerTransportAckReceived = true;
            else
                ClientTransportAckReceived = true;
        }

        protected override void ReleaseWhenReliabilityAchieved()
        {
            if (ClientTransportAckReceived && BrokerTransportAckReceived)
                _waitForReliabilityConditionsToBeFulfilled.Set();
        }
    }


    internal class FireAndForget : SendingReliabilityStrategy
    {
        public override void SendOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message)
        {
           endpointManager.SendMessage(message);
        }

        public override void PublishOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message)
        {
           endpointManager.PublishMessage(message);
        }

        public override void RouteOn(IEndpointManager endpointManager, ISendingStrategyManager strategyManager, ITransportMessage message)
        {
          endpointManager.RouteMessage(message);
        }

        public override void CheckMessage(ITransportMessage message)
        {

        }

        protected override void ReleaseWhenReliabilityAchieved()
        {
            _waitForReliabilityConditionsToBeFulfilled.Set();
        }
    }
}