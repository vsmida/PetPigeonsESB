using System;
using System.Threading;
using PersistenceService.Commands;
using Shared;

namespace ZmqServiceBus.Transport
{
    public interface IReliabilityStrategy
    {
        bool ClientTransportAckReceived { get; set; }
        bool BrokerTransportAckReceived { get; set; }
        bool ClientDispatchAckReceived { get; set; }
        bool ClientDispatchSuccessful { get; set; }
        void SendOn(ITransport transport, ITransportMessage message);
        void PublishOn(ITransport transport, ITransportMessage message);
        void RouteOn(ITransport transport, ITransportMessage message);
        void CheckMessage(ITransportMessage message);
    }

    public interface IReliabilityStrategyFactory
    {
        IReliabilityStrategy GetStrategy(MessageOptions messageOptions);
    }

    public class ReliabilityStrategyFactory : IReliabilityStrategyFactory
    {
        public IReliabilityStrategy GetStrategy(MessageOptions messageOptionses)
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
    }


    public abstract class ReliabilityStrategy : IReliabilityStrategy
    {

        private bool _clientTransportAckReceived;
        private bool _brokerTransportAckReceived;
        private bool _clientDispatchAckReceived;
        private bool _clientDispatchSuccessful;
        protected readonly AutoResetEvent _waitForReliabilityConditionsToBeFulfilled = new AutoResetEvent(false);

        protected ReliabilityStrategy()
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

        public abstract void SendOn(ITransport transport, ITransportMessage message);
        public abstract void PublishOn(ITransport transport, ITransportMessage message);
        public abstract void RouteOn(ITransport transport, ITransportMessage message);
        public abstract void CheckMessage(ITransportMessage message);


        protected abstract void ReleaseWhenReliabilityAchieved();

    }

    internal class WaitForClientOrBrokerTransportAck : ReliabilityStrategy
    {
        private readonly string _brokerPeerName;

        public WaitForClientOrBrokerTransportAck(string brokerPeerName)
        {
            _brokerPeerName = brokerPeerName;
        }

        public override void SendOn(ITransport transport, ITransportMessage message)
        {
            transport.SendMessage(message);
            transport.RouteMessage(new TransportMessage(typeof(PersistMessageCommand).FullName, _brokerPeerName, message.MessageIdentity, Serializer.Serialize(message)));
           
        }

        public override void PublishOn(ITransport transport, ITransportMessage message)
        {
            throw new NotImplementedException();
        }

        public override void RouteOn(ITransport transport, ITransportMessage message)
        {
            throw new NotImplementedException();
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

    internal class FireAndForget : ReliabilityStrategy
    {
        public override void SendOn(ITransport transport, ITransportMessage message)
        {
            transport.SendMessage(message);
        }

        public override void PublishOn(ITransport transport, ITransportMessage message)
        {
            transport.PublishMessage(message);
        }

        public override void RouteOn(ITransport transport, ITransportMessage message)
        {
           
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