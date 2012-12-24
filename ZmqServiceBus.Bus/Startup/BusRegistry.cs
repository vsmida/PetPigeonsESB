using StructureMap.Configuration.DSL;
using ZeroMQ;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Handlers;
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
             ForSingletonOf<IObjectFactory>().Use<ObjectFactory>();
             For<IAssemblyScanner>().Use<AssemblyScanner>();
             ForSingletonOf<ZmqTransportConfiguration>().Use<ZmqTransportConfigurationRandomPort>();
             ForSingletonOf<IWireSendingTransport>().Add<ZmqPushWireSendingTransport>().Ctor<ZmqContext>().Is(ZmqContext.Create());
             ForSingletonOf<IDataReceiver>().Use<ZmqDataReceiver>().Ctor<ZmqContext>().Is(ZmqContext.Create());
             ForSingletonOf<IPeerManager>().Use<PeerManager>();
             ForSingletonOf<IDataSender>().Use<DataSender>();
             ForSingletonOf<IReliabilityStrategyFactory>().Use<ReliabilityStrategyFactory>();
             ForSingletonOf<IReceptionLayer>().Use<ReceptionLayer>();
             ForSingletonOf<IMessageSender>().Use<MessageSender>();
             ForSingletonOf<IMessageDispatcher>().Use<MessageDispatcher>();
             ForSingletonOf<IPersistenceSynchronizer>().Use<BrokerPersistenceSynchronizer>();
             ForSingletonOf<IStartupStrategyManager>().Use<StartupStrategyManager>();
             ForSingletonOf<ISendingStrategyStateManager>().Use<SendingStrategyStateManager>();
             ForSingletonOf<IMessageOptionsRepository>().Use<MessageOptionsRepository>();
             ForSingletonOf<ISubscriptionManager>().Use<SubscriptionManager>();
             ForSingletonOf<ICallbackRepository>().Use<CallbackRepository>();
             ForSingletonOf<IBusBootstrapper>().Use<BusBootstrapper>();
             ForSingletonOf<IBusBootstrapperConfiguration>().Use<BusBootstrapperConfiguration>();
             ForSingletonOf<CompletionMessagesHandler>().Use(
                 ctx => new CompletionMessagesHandler(ctx.GetInstance<ICallbackRepository>()));
             ForSingletonOf<IBus>().Use<InternalBus>();
             
         }
    }
}