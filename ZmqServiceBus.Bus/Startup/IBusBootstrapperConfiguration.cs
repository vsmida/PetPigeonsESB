namespace ZmqServiceBus.Bus.Startup
{
    public interface IBusBootstrapperConfiguration
    {
        string DirectoryServiceCommandEndpoint { get; }
        string DirectoryServiceEventEndpoint { get; }
    }
}