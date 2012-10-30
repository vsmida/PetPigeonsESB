using System;
using System.Threading;

namespace ZmqServiceBus.Transport
{
    public interface IReliabilityStrategy
    {
        bool ClientTransportAckReceived { get; set; }
        bool BrokerTransportAckReceived { get; set; }
        bool ClientDispatchAckReceived { get; set; }
        bool ClientDispatchSuccessful { get; set; }
        WaitHandle WaitForReliabilityConditionsToBeFulfilled { get; }
    }

    public interface IReliabilityStrategyFactory
    {
        IReliabilityStrategy GetStrategy(ReliabilityOption option);
    }

    public class ReliabilityStrategyFactory : IReliabilityStrategyFactory
    {
        public IReliabilityStrategy GetStrategy(ReliabilityOption option)
        {
            switch (option)
            {
                case ReliabilityOption.FireAndForget:
                    return new FireAndForget();
                    break;
                //case ReliabilityOption.SendToClientAndBrokerNoAck:
                //    break;
                case ReliabilityOption.SomeoneReceivedMessageOnTransport:
                    return new WaitForClientOrBrokerTransportAck();
                    break;
                //case ReliabilityOption.ClientAndBrokerReceivedOnTransport:
                //    break;
                default:
                    throw new ArgumentOutOfRangeException("option");
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

        public WaitHandle WaitForReliabilityConditionsToBeFulfilled
        {
            get { return _waitForReliabilityConditionsToBeFulfilled; }
        }

        protected abstract void ReleaseWhenReliabilityAchieved();

    }

    internal class WaitForClientOrBrokerTransportAck : ReliabilityStrategy
    {
        protected override void ReleaseWhenReliabilityAchieved()
        {
            if (ClientTransportAckReceived && BrokerTransportAckReceived)
                _waitForReliabilityConditionsToBeFulfilled.Set();
        }
    }

    internal class FireAndForget : ReliabilityStrategy
    {
        protected override void ReleaseWhenReliabilityAchieved()
        {
            _waitForReliabilityConditionsToBeFulfilled.Set();
        }
    }
}