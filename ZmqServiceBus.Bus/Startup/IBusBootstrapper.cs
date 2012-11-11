using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Bus.Startup
{
    public interface IBusBootstrapper
    {
        void BootStrapTopology();
    }

    public class BusBootstrapper : IBusBootstrapper
    {
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly ITransportConfiguration _transportConfiguration;
        private readonly IBusBootstrapperConfiguration _bootstrapperConfiguration;
        private readonly IReliabilityLayer _reliabilityLayer;
        //private readonly I

        public void BootStrapTopology()
        {
            throw new System.NotImplementedException();
        }
    }
}