namespace ZmqServiceBus.Bus
{
    public interface IBusConfiguration
    {
        string DirectoryServiceCommandEndpoint { get; }
        string DirectoryServiceEventEndpoint { get; }
    }
}