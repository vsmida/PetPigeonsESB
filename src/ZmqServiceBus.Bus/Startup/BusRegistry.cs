using StructureMap;
using StructureMap.Configuration.DSL;
using ZeroMQ;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Handlers;
using ZmqServiceBus.Bus.Subscriptions;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
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
             ForSingletonOf<IWireReceiverTransport>().Use<ZmqWireDataReceiver>().Ctor<ZmqContext>().Is(ZmqContext.Create());
             ForSingletonOf<IDataReceiver>().Use<DataReceiver>();
             ForSingletonOf<ISavedMessagesStore>().Use<SavedMessagesStore>();
             ForSingletonOf<IHeartbeatingConfiguration>().Use<DummyHeartbeatingConfig>();
             ForSingletonOf<IPeerManager>().Use<PeerManager>();
             ForSingletonOf<INetworkSender>().Use<NetworkSender>();
             ForSingletonOf<IReliabilityStrategyFactory>().Use<ReliabilityStrategyFactory>();
             ForSingletonOf<IMessageSender>().Use<MessageSender>();
             For<IMessageDispatcher>().Use<MessageDispatcher>();
             For<IPeerConfiguration>().Use<PeerConfiguration>();
             ForSingletonOf<IMessageOptionsRepository>().Use<MessageOptionsRepository>();
             ForSingletonOf<ISubscriptionManager>().Use<SubscriptionManager>();
             ForSingletonOf<ICallbackRepository>().Use<CallbackRepository>();
             ForSingletonOf<IHeartbeatManager>().Use<HeartbeatManager>();
             ForSingletonOf<IBusBootstrapper>().Use<BusBootstrapper>();
             ForSingletonOf<IBusBootstrapperConfiguration>().Use<BusBootstrapperConfiguration>();
             ForSingletonOf<IBus>().Use<InternalBus>();
             ForSingletonOf<IReplier>().Use(ctx => ctx.GetInstance<InternalBus>());
            

         }
    }
}