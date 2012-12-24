namespace ZmqServiceBus.Bus.Startup
{
    public interface IBusBootstrapperConfiguration
    {
        string DirectoryServiceEndpoint { get; }
        string DirectoryServiceName { get; }
    }
}