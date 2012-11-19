using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

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
        private readonly IReceptionLayer _receptionLayer;
        //private readonly I

        public void BootStrapTopology()
        {
            throw new System.NotImplementedException();
        }
    }
}