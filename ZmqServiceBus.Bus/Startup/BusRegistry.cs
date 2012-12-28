using StructureMap;
using StructureMap.Configuration.DSL;
using ZeroMQ;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Handlers;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using IMessageSender = ZmqServiceBus.Bus.Transport.SendingPipe.IMessageSender;

namespace ZmqServiceBus.Bus.Startup
{
    public class BusRegistry : Registry
    {
         public BusRegistry()
         {
             For<IAssemblyScanner>().Use<AssemblyScanner>();
             ForSingletonOf<ZmqTransportConfiguration>().Use<ZmqTransportConfigurationRandomPort>();
             ForSingletonOf<IWireSendingTransport>().Add<ZmqPushWireSendingTransport>().Ctor<ZmqContext>().Is(ZmqContext.Create());
             ForSingletonOf<IWireReceiverTransport>().Use<ZmqDataReceiver>().Ctor<ZmqContext>().Is(ZmqContext.Create());
             ForSingletonOf<IDataReceiver>().Use<DataReceiver>();
             ForSingletonOf<IHeartbeatManager>().Use<HeartbeatManager>();
             ForSingletonOf<IHeartbeatingConfiguration>().Use<DummyHeartbeatingConfig>();
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
             ForSingletonOf<IBus>().Use<InternalBus>();
             ForSingletonOf<IReplier>().Use(ctx => ctx.GetInstance<InternalBus>());

         }
    }
}