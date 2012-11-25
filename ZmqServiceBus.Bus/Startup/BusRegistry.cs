using StructureMap.Configuration.DSL;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;

namespace ZmqServiceBus.Bus.Startup
{
    public class BusRegistry : Registry
    {
         public BusRegistry()
         {
             For<IObjectFactory>().Use<ObjectFactory>();
             For<TransportConfiguration>().Use<TransportConfigurationRandomPort>();
             For<IZmqSocketManager>().Use<ZmqSocketManager>().Ctor<ZmqContext>().Is(ZmqContext.Create());
             For<IEndpointManager>().Use<EndpointManager>();
             For<IPeerManager>().Use<PeerManager>();
             For<IReliabilityStrategyFactory>().Use<ReliabilityStrategyFactory>();
             For<IReceptionLayer>().Use<ReceptionLayer>();
             For<IMessageSender>().Use<MessageSender>();
             For<IStartupStrategyManager>().Use<StartupStrategyManager>();
             For<ISendingStrategyStateManager>().Use<SendingStrategyStateManager>();
             For<IMessageOptionsRepository>().Use<MessageOptionsRepository>();
             For<ISubscriptionManager>().Use<SubscriptionManager>();
             For<IBus>().Use<InternalBus>();
             
         }
    }
}