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
    public class InternalBus : IBus, IReplier
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

        public InternalBus(IMessageSender messageSender, IBusBootstrapper busBootstrapper, IDataReceiver dataReceiver, HandlingProcessorStandard handlingProcessorStandard, HandlingProcessorInfrastructure handlingProcessorInfrastructure, PersistenceSynchronizationProcessor networkProcessor, MessageTargetsHandler messageTargetsHandler, NetworkSender networkSender)
        {
            _messageSender = messageSender;
            _busBootstrapper = busBootstrapper;
            _dataReceiver = dataReceiver;
            _handlingProcessorStandard = handlingProcessorStandard;
            _handlingProcessorInfrastructure = handlingProcessorInfrastructure;
            _networkProcessor = networkProcessor;
            _messageTargetsHandler = messageTargetsHandler;
            _networkSender = networkSender;
            _networkInputDisruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),new MultiThreadedClaimStrategy(32768),new BlockingWaitStrategy(), TaskScheduler.Default);
            _infrastructureInputDisruptor = new Disruptor<InboundInfrastructureEntry>(() => new InboundInfrastructureEntry(), new SingleThreadedClaimStrategy(512), new BlockingWaitStrategy(), TaskScheduler.Default);
            _normalMessagesInputDisruptor = new Disruptor<InboundBusinessMessageEntry>(() => new InboundBusinessMessageEntry(), new SingleThreadedClaimStrategy(32768), new BlockingWaitStrategy(), TaskScheduler.Default);
            _outputDisruptor = new Disruptor<OutboundDisruptorEntry>(() => new OutboundDisruptorEntry(), new MultiThreadedClaimStrategy(65536), new BlockingWaitStrategy(), TaskScheduler.Default);
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
            _dataReceiver.Dispose();
            
            _networkInputDisruptor.Shutdown();
            _normalMessagesInputDisruptor.Shutdown();
            _infrastructureInputDisruptor.Shutdown();

            _outputDisruptor.Shutdown();
            _networkSender.Dispose();
        }
    }
}