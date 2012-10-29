namespace ZmqServiceBus.Transport
{
    public interface IReliabilityLayer
    {
        void RegisterMessageReliabilitySetting<T>(ReliabilityStrategy option);
        void Send<T>(T message);
    }

    public class ReliabilityLayer : IReliabilityLayer
    {
        public void RegisterMessageReliabilitySetting<T>(ReliabilityStrategy option)
        {
            
        }

        public void Send<T>(T message)
        {
           
        }
    }
}