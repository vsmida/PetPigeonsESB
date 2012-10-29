namespace ZmqServiceBus.Transport
{
    public interface IReliabilityInfo
    {
        bool ClientTransportAckReceived { get; set; }
        bool BrokerTransportAckReceived { get; set; }
        bool ClientDispatchAckReceived { get; set; }
        bool ClientDispatchSuccessful { get; set; }
    }

   public class ReliabilityInfo : IReliabilityInfo
    {
        public bool ClientTransportAckReceived { get; set; }
        public bool BrokerTransportAckReceived { get; set; }
        public bool ClientDispatchAckReceived { get; set; }
        public bool ClientDispatchSuccessful { get; set; }
    }
}