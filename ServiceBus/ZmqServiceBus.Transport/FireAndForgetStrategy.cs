namespace ZmqServiceBus.Transport
{
    public class FireAndForgetStrategy : IQosStrategy
    {
        public void WaitForQosAssurancesToBeFulfilled(ITransportMessage message)
        {
            
        }
    }
}