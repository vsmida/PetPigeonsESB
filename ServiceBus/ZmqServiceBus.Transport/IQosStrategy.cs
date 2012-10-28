namespace ZmqServiceBus.Transport
{
    public interface IQosStrategy
    {
        void WaitForQosAssurancesToBeFulfilled(ITransportMessage message);

    }
}