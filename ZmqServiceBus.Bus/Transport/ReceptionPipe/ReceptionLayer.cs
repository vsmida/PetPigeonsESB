using System;
using System.Collections.Concurrent;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class ReceptionLayer : IReceptionLayer
    {
        private readonly ISendingStrategyStateManager _sendingStrategyStateManager;
        private readonly IStartupStrategyManager _startupStrategyManager;
        private readonly BlockingCollection<Transport.IReceivedTransportMessage> _messagesToForward = new BlockingCollection<Transport.IReceivedTransportMessage>();
        private readonly IEndpointManager _endpointManager;
        public event Action<Transport.IReceivedTransportMessage> OnMessageReceived = delegate { };
        public void Initialize()
        {
            _endpointManager.Initialize();
        }

        private volatile bool _running = true;


        public ReceptionLayer(IEndpointManager endpointManager, ISendingStrategyStateManager sendingStrategyStateManager, IStartupStrategyManager startupStrategyManager)
        {
            _endpointManager = endpointManager;
            _sendingStrategyStateManager = sendingStrategyStateManager;
            _startupStrategyManager = startupStrategyManager;
            _endpointManager.OnMessageReceived += OnEndpointManagerMessageReceived;
            CreateEventThread();
        }


        private void CreateEventThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             Transport.IReceivedTransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromSeconds(1)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }
                                     }).Start();
        }

        private void OnEndpointManagerMessageReceived(Transport.IReceivedTransportMessage receivedTransportMessage)
        {
            _sendingStrategyStateManager.CheckMessage(receivedTransportMessage);

            if (IsTransportAck(receivedTransportMessage))
                return;
            foreach (var transportMessage in _startupStrategyManager.CheckMessage(receivedTransportMessage))
            {
                OnMessageReceived(transportMessage);
            }
        }

        private static bool IsTransportAck(Transport.IReceivedTransportMessage receivedTransportMessage)
        {
            return receivedTransportMessage.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName;
        }


        public void Dispose()
        {
            _running = false;
            _endpointManager.Dispose();
        }
    }
}