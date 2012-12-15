using System;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class MessageSender : IMessageSender
    {
        private readonly IEndpointManager _endpointManager;
        private readonly IMessageOptionsRepository _messageOptionsRepository;
        private readonly IReliabilityStrategyFactory _strategyFactory;
        private readonly ICallbackRepository _callbackRepository;

        public MessageSender(IEndpointManager endpointManager, IMessageOptionsRepository messageOptionsRepository, IReliabilityStrategyFactory strategyFactory, ICallbackRepository callbackRepository)
        {
            _endpointManager = endpointManager;
            _messageOptionsRepository = messageOptionsRepository;
            _strategyFactory = strategyFactory;
            _callbackRepository = callbackRepository;
        }

        public IBlockableUntilCompletion Send(ICommand command, ICompletionCallback callback = null)
        {
            var sendingStrat =_strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(command.GetType().FullName));
            ISendingBusMessage sendingMessage = GetTransportMessage(command);
            var callbackToRegister = callback ?? new DefaultCompletionCallback();
            _callbackRepository.RegisterCallback(sendingMessage.MessageIdentity, callbackToRegister);
            sendingStrat.SendOn(_endpointManager, sendingMessage);
            return callbackToRegister;
          }

        private ISendingBusMessage GetTransportMessage(IMessage message)
        {
            return new SendingBusMessage(message.GetType().FullName, Guid.NewGuid(), Serializer.Serialize(message));
        }

        public void Publish(IEvent message)
        {
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingBusMessage sendingMessage = GetTransportMessage(message);
            sendingStrat.PublishOn(_endpointManager, sendingMessage);
        }

        public void Route(IMessage message, string peerName)
        {
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingBusMessage sendingMessage = GetTransportMessage(message);
            sendingStrat.RouteOn(_endpointManager, sendingMessage, peerName);
        }
    }
}