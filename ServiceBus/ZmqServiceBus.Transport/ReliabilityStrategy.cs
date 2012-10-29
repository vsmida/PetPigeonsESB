using System.Threading;

namespace ZmqServiceBus.Transport
{
    public abstract class ReliabilityStrategy
    {

        public static ReliabilityStrategy FireAndForget {get{return new FireAndForget();}}
        public static ReliabilityStrategy WaitForClientOrBrokerTransportAck { get { return new WaitForClientOrBrokerTransportAck(); } }

        private bool _clientTransportAckReceived;
        private bool _brokerTransportAckReceived;
        private bool _clientDispatchAckReceived;
        private bool _clientDispatchSuccessful;
        protected readonly AutoResetEvent _waitForReliabilityConditionsToBeFulfilled = new AutoResetEvent(false);

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

    internal class FireAndForget : ReliabilityStrategy
    {
        protected override void ReleaseWhenReliabilityAchieved()
        {
            _waitForReliabilityConditionsToBeFulfilled.Set();
        }
    }

    internal class WaitForClientOrBrokerTransportAck : ReliabilityStrategy
    {
        protected override void ReleaseWhenReliabilityAchieved()
        {
            if(ClientTransportAckReceived && BrokerTransportAckReceived)
            _waitForReliabilityConditionsToBeFulfilled.Set();
        }
    }
}