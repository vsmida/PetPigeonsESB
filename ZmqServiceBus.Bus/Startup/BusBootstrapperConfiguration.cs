using System.Configuration;

namespace ZmqServiceBus.Bus.Startup
{
    public class BusBootstrapperConfiguration : IBusBootstrapperConfiguration
    {
        public string DirectoryServiceCommandEndpoint { get { return ConfigurationManager.AppSettings["DirectoryServiceCommandEndpoint"]; } }
        public string DirectoryServiceEventEndpoint { get { return ConfigurationManager.AppSettings["DirectoryServiceEventEndpoint"]; } }
        public string DirectoryServiceName { get { return ConfigurationManager.AppSettings["DirectoryServiceName"]; } }
    }
}