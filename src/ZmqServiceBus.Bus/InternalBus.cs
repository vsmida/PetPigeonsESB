using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.DisruptorEventHandlers;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using Disruptor.Dsl;

namespace ZmqServiceBus.Bus
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
        private readonly Disruptor<InboundInfrastructureEntry> _infrastructureInputDisruptor;
        private readonly Disruptor<InboundBusinessMessageEntry> _normalMessagesInputDisruptor;
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
            _infrastructureInputDisruptor = new Disruptor<InboundInfrastructureEntry>(() => new InboundInfrastructureEntry(), new SingleThreadedClaimStrategy(_queueConfiguration.InfrastructureQueueSize), new BlockingWaitStrategy(), TaskScheduler.Default);
            _normalMessagesInputDisruptor = new Disruptor<InboundBusinessMessageEntry>(() => new InboundBusinessMessageEntry(), new SingleThreadedClaimStrategy(_queueConfiguration.StandardDispatchQueueSize), new BlockingWaitStrategy(), TaskScheduler.Default);
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

            _infrastructureInputDisruptor.HandleEventsWith(_handlingProcessorInfrastructure);
            _infrastructureInputDisruptor.Start();
            _normalMessagesInputDisruptor.HandleEventsWith(_handlingProcessorStandard);
            _normalMessagesInputDisruptor.Start();
            _networkProcessor.Initialize(_infrastructureInputDisruptor.RingBuffer, _normalMessagesInputDisruptor.RingBuffer);
            _networkInputDisruptor.HandleEventsWith(_networkProcessor);
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
            
            //Thread.Sleep(200); // re-entrant command problem, should wait for infra/strandard ringbuffers to have full capacity once then shutdown networkDisruptor?
            while(!_networkInputDisruptor.RingBuffer.HasAvailableCapacity(_queueConfiguration.NetworkQueueSize) 
                || !_normalMessagesInputDisruptor.RingBuffer.HasAvailableCapacity(_queueConfiguration.StandardDispatchQueueSize)
                || !_infrastructureInputDisruptor.RingBuffer.HasAvailableCapacity(_queueConfiguration.InfrastructureQueueSize)
                || !_outputDisruptor.RingBuffer.HasAvailableCapacity(_queueConfiguration.OutboundQueueSize))
            {
                Thread.Sleep(1);
            }

            _networkInputDisruptor.Shutdown();
            _normalMessagesInputDisruptor.Shutdown();
            _infrastructureInputDisruptor.Shutdown();
            _messageSender.Dispose();
            _outputDisruptor.Shutdown();
       //     Thread.Sleep(100); //what the fuck? everybody should ahve shut down? is shutdown not synchronous?
            _networkSender.Dispose();
        }
    }
}