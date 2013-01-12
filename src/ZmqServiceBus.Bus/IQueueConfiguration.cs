namespace ZmqServiceBus.Bus
{
    public interface IQueueConfiguration
    {
        int InfrastructureQueueSize { get; }
        int NetworkQueueSize { get; }
        int StandardDispatchQueueSize { get; }
        int OutboundQueueSize { get; }
    }

    public class DefaultQueueConfiguration : IQueueConfiguration
    {
        public int InfrastructureQueueSize
        {
            get { return 128; }
        }

        public int NetworkQueueSize
        {
            get { return 16384; }
        }

        public int StandardDispatchQueueSize
        {
            get { return 16384; }
        }

        public int OutboundQueueSize
        {
            get { return 16384; }
        }
    }
}