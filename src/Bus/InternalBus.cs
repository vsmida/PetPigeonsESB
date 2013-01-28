using System.Threading;
using System.Threading.Tasks;
using Bus.Dispatch;
using Bus.DisruptorEventHandlers;
using Bus.MessageInterfaces;
using Bus.Startup;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Disruptor.Dsl;

namespace Bus
{
    class InternalBus : IBus, IReplier
    {
        private readonly HandlingProcessorStandard _handlingProcessorStandard;
        private readonly HandlingProcessorInfrastructure _handlingProcessorInfrastructure;
        private readonly MessageTargetsHandler _messageTargetsHandler;
        private readonly NetworkSender _networkSender;
        private readonly PersistenceSynchronizationProcessor _networkProcessor;
        private readonly IMessageSender _messageSender;
        private readonly IBusBootstrapper _busBootstrapper;
        private readonly IDataReceiver _dataReceiver;
        private readonly Disruptor<InboundMessageProcessingEntry> _networkInputDisruptor;
        private readonly Disruptor<OutboundDisruptorEntry> _outputDisruptor;
        private readonly IHeartbeatManager _heartbeatManager;
        private readonly IQueueConfiguration _queueConfiguration;

        public InternalBus(IMessageSender messageSender, IBusBootstrapper busBootstrapper, IDataReceiver dataReceiver, HandlingProcessorStandard handlingProcessorStandard, HandlingProcessorInfrastructure handlingProcessorInfrastructure, PersistenceSynchronizationProcessor networkProcessor, MessageTargetsHandler messageTargetsHandler, NetworkSender networkSender, IHeartbeatManager heartbeatManager, IQueueConfiguration queueConfiguration)
        {
            _messageSender = messageSender;
            _busBootstrapper = busBootstrapper;
            _dataReceiver = dataReceiver;
            _handlingProcessorStandard = handlingProcessorStandard;
            _handlingProcessorInfrastructure = handlingProcessorInfrastructure;
            _networkProcessor = networkProcessor;
            _messageTargetsHandler = messageTargetsHandler;
            _networkSender = networkSender;
            _heartbeatManager = heartbeatManager;
            _queueConfiguration = queueConfiguration;
            _networkInputDisruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),new MultiThreadedClaimStrategy(_queueConfiguration.NetworkQueueSize),new BlockingWaitStrategy(), TaskScheduler.Default);
            _outputDisruptor = new Disruptor<OutboundDisruptorEntry>(() => new OutboundDisruptorEntry(), new MultiThreadedClaimStrategy(_queueConfiguration.OutboundQueueSize), new BlockingWaitStrategy(), TaskScheduler.Default);
        }

        public IBlockableUntilCompletion Send(ICommand command)
        {
            var blockableUntilCompletion = _messageSender.Send(command);
            return blockableUntilCompletion;
        }

        public void Publish(IEvent message)
        {
            _messageSender.Publish(message);
        }

        public void Initialize()
        {
            _networkInputDisruptor.HandleEventsWith(_networkProcessor).Then(_handlingProcessorInfrastructure,
                                                                            _handlingProcessorStandard);
            _networkInputDisruptor.Start();
            _dataReceiver.Initialize(_networkInputDisruptor.RingBuffer);

            _networkSender.Initialize();
            _outputDisruptor.HandleEventsWith(_messageTargetsHandler).Then(_networkSender);
            _messageSender.Initialize(_outputDisruptor.RingBuffer);
            _outputDisruptor.Start();
            

            _busBootstrapper.BootStrapTopology();
        }

        public void Reply(IMessage message)
        {
            _messageSender.Route(message, MessageContext.PeerName);
        }

        public void Dispose()
        {
            _heartbeatManager.Dispose();
            _dataReceiver.Dispose();
            
            while(!_networkInputDisruptor.RingBuffer.HasAvailableCapacity(_queueConfiguration.NetworkQueueSize) 
                || !_outputDisruptor.RingBuffer.HasAvailableCapacity(_queueConfiguration.OutboundQueueSize))
            {
                Thread.Sleep(1);
            }

            _networkInputDisruptor.Shutdown();
            _messageSender.Dispose();
            _outputDisruptor.Shutdown();
            _networkSender.Dispose();
        }
    }
}