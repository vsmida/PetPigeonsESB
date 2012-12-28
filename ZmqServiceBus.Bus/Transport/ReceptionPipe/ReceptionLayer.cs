using System;
using System.Collections.Concurrent;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class ReceptionLayer : IReceptionLayer
    {
        private readonly ISendingStrategyStateManager _sendingStrategyStateManager;
        private readonly IStartupStrategyManager _startupStrategyManager;
        private readonly IDataReceiver _dataReceiver;
        private readonly IMessageSender _messageSender;
        private readonly IMessageOptionsRepository _messageOptionsRepository;
        public event Action<IReceivedTransportMessage> OnMessageReceived = delegate { };
        public void Initialize()
        {
            _dataReceiver.Initialize();
        }



        public ReceptionLayer(IDataReceiver dataReceiver, ISendingStrategyStateManager sendingStrategyStateManager, IStartupStrategyManager startupStrategyManager, IMessageOptionsRepository messageOptionsRepository, IMessageSender messageSender)
        {
            _dataReceiver = dataReceiver;
            _sendingStrategyStateManager = sendingStrategyStateManager;
            _startupStrategyManager = startupStrategyManager;
            _messageOptionsRepository = messageOptionsRepository;
            _messageSender = messageSender;
            _dataReceiver.OnMessageReceived += OnEndpointManagerMessageReceived;
        }



        private void OnEndpointManagerMessageReceived(IReceivedTransportMessage receivedTransportMessage)
        {
            _sendingStrategyStateManager.CheckMessage(receivedTransportMessage);

            if (IsTransportAck(receivedTransportMessage))
                return;

            var options = _messageOptionsRepository.GetOptionsFor(receivedTransportMessage.MessageType);
            if (options.ReliabilityInfo.ShouldAck())
                _messageSender.Route(new ReceivedOnTransportAcknowledgement(receivedTransportMessage.MessageIdentity),
                                     receivedTransportMessage.PeerName);

            foreach (var transportMessage in _startupStrategyManager.CheckMessage(receivedTransportMessage))
            {
                OnMessageReceived(transportMessage);
            }
        }

        private static bool IsTransportAck(IReceivedTransportMessage receivedTransportMessage)
        {
            return receivedTransportMessage.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName;
        }


        public void Dispose()
        {
            _dataReceiver.Dispose();
        }
    }
}