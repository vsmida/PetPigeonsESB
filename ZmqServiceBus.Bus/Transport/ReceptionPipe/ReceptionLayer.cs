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
        private readonly IDataReceiver _dataReceiver;
        public event Action<IReceivedTransportMessage> OnMessageReceived = delegate { };
        public void Initialize()
        {
            _dataReceiver.Initialize();
        }



        public ReceptionLayer(IDataReceiver dataReceiver, ISendingStrategyStateManager sendingStrategyStateManager, IStartupStrategyManager startupStrategyManager)
        {
            _dataReceiver = dataReceiver;
            _sendingStrategyStateManager = sendingStrategyStateManager;
            _startupStrategyManager = startupStrategyManager;
            _dataReceiver.OnMessageReceived += OnEndpointManagerMessageReceived;
        }



        private void OnEndpointManagerMessageReceived(IReceivedTransportMessage receivedTransportMessage)
        {
            _sendingStrategyStateManager.CheckMessage(receivedTransportMessage);

            if (IsTransportAck(receivedTransportMessage))
                return;
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