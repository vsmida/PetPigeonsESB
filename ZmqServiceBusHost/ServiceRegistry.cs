using StructureMap.Configuration.DSL;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBusHost
{
    public class ServiceRegistry : Registry
    {
         public ServiceRegistry()
         {
             For<IObjectFactory>().Use<ObjectFactory>();
             For<IZmqSocketManager>().Use<ZmqSocketManager>();
             For<IEndpointManager>().Use<EndpointManager>();
             For<IReliabilityLayer>().Use<ReliabilityLayer>();


         }
    }
}