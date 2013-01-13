using System.Configuration;

namespace Bus.Startup
{
    public class BusBootstrapperConfiguration : IBusBootstrapperConfiguration
    {
        public string DirectoryServiceEndpoint { get; private set; }
        public string DirectoryServiceName { get { return ConfigurationManager.AppSettings["DirectoryServiceName"]; } }
    }
}