using Bus.Dispatch;
using Bus.DisruptorEventHandlers;
using Bus.Handlers;
using Bus.Subscriptions;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;
using ZeroMQ;

namespace Bus.Startup
{
    public class BusRegistry : Registry
    {
         public BusRegistry()
         {
             For<IAssemblyScanner>().LifecycleIs(new UniquePerRequestLifecycle()).Use<AssemblyScanner>();
             ForSingletonOf<ZmqTransportConfiguration>().Use<ZmqTransportConfigurationRandomPort>();
             ForSingletonOf<IQueueConfiguration>().Use<DefaultQueueConfiguration>();
             var zmqContext = ZmqContext.Create();
             ForSingletonOf<IWireSendingTransport>().Add<ZmqPushWireSendingTransport>().Ctor<ZmqContext>().Is(zmqContext);
             var context = ZmqContext.Create();
             ForSingletonOf<IWireReceiverTransport>().Use<ZmqWireDataReceiver>().Ctor<ZmqContext>().Is(context);
             ForSingletonOf<IDataReceiver>().Use<DataReceiver>();
             ForSingletonOf<ISavedMessagesStore>().Use<SavedMessagesStore>();
             ForSingletonOf<IHeartbeatingConfiguration>().Use<DummyHeartbeatingConfig>();
             ForSingletonOf<IPeerManager>().Use<PeerManager>();
             ForSingletonOf<INetworkSender>().Use<NetworkSender>();
             ForSingletonOf<IMessageSender>().Use<MessageSender>();
             For<IMessageDispatcher>().LifecycleIs(new UniquePerRequestLifecycle()).Use<MessageDispatcher>();
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