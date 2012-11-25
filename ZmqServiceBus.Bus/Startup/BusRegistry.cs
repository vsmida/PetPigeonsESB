using StructureMap.Configuration.DSL;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Startup
{
    public class BusRegistry : Registry
    {
         public BusRegistry()
         {
             For<IObjectFactory>().Use<ObjectFactory>();
             For<IZmqSocketManager>().Use<ZmqSocketManager>();
             For<IEndpointManager>().Use<EndpointManager>();
             For<IReceptionLayer>().Use<ReceptionLayer>();
             For<IMessageSender>().Use<MessageSender>();
             For<IStartupStrategyManager>().Use<StartupStrategyManager>();
             For<IMessageOptionsRepository>().Use<MessageOptionsRepository>();
             For<ISubscriptionManager>().Use<SubscriptionManager>();
             For<IBus>().Use<InternalBus>();
         }
    }
}