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
        private readonly ICallbackManager _callbackManager;

        public MessageSender(IEndpointManager endpointManager, IMessageOptionsRepository messageOptionsRepository, IReliabilityStrategyFactory strategyFactory, ICallbackManager callbackManager)
        {
            _endpointManager = endpointManager;
            _messageOptionsRepository = messageOptionsRepository;
            _strategyFactory = strategyFactory;
            _callbackManager = callbackManager;
        }

        public IBlockableUntilCompletion Send(ICommand command, ICompletionCallback callback = null)
        {
            var sendingStrat =_strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(command.GetType().FullName));
            ISendingTransportMessage sendingMessage = GetTransportMessage(command);
            var callbackToRegister = callback ?? new DefaultCompletionCallback();
            _callbackManager.RegisterCallback(sendingMessage.MessageIdentity, callbackToRegister);
            sendingStrat.SendOn(_endpointManager, sendingMessage);
            return callbackToRegister;
          }

        private ISendingTransportMessage GetTransportMessage(IMessage message)
        {
            return new SendingTransportMessage(message.GetType().FullName, Guid.NewGuid(), Serializer.Serialize(message));
        }

        public void Publish(IEvent message)
        {
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingTransportMessage sendingMessage = GetTransportMessage(message);
            sendingStrat.PublishOn(_endpointManager, sendingMessage);
        }

        public void Route(IMessage message, string peerName)
        {
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingTransportMessage sendingMessage = GetTransportMessage(message);
            sendingStrat.RouteOn(_endpointManager, sendingMessage, peerName);
        }
    }
}