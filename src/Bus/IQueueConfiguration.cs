namespace Bus
{
    public interface IQueueConfiguration
    {
        int InboundQueueSize { get; }
        int OutboundQueueSize { get; }
    }

    public class DefaultQueueConfiguration : IQueueConfiguration
    {
        public int InboundQueueSize
        {
            get { return 32768; }
        }

        public int OutboundQueueSize
        {
            get { return 32768; }
        }
    }
}