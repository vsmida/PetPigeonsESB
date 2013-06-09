namespace Bus.Startup
{
    public interface IBusBootstrapperConfiguration
    {
        string DirectoryServiceEndpoint { get; }
        string DirectoryServiceName { get; }
        PeerId DirectoryServiceId { get; }
    }
}