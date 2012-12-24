using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;
using System.Linq;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class MessageSender : IMessageSender
    {
        private enum SendAction
        {
            Send, Publish, Route
        }

        private class ItemToSend
        {
            public SendAction SendAction;
            public IMessage Message;
            public ICompletionCallback Callback;
            public string PeerName;
        }

        private readonly IMessageOptionsRepository _messageOptionsRepository;
        private readonly IReliabilityStrategyFactory _strategyFactory;
        private readonly ICallbackRepository _callbackRepository;
        private readonly BlockingCollection<ItemToSend> _itemsToSend = new BlockingCollection<ItemToSend>();
        private readonly IPeerManager _peerManager;

        public MessageSender(IMessageOptionsRepository messageOptionsRepository, IReliabilityStrategyFactory strategyFactory, ICallbackRepository callbackRepository, IPeerManager peerManager)
        {
            _messageOptionsRepository = messageOptionsRepository;
            _strategyFactory = strategyFactory;
            _callbackRepository = callbackRepository;
            _peerManager = peerManager;

            new BackgroundThread(MainSendingLoop).Start();
        }

        private void MainSendingLoop()
        {
            foreach (var itemToSend in _itemsToSend.GetConsumingEnumerable())
            {
                switch (itemToSend.SendAction)
                {
                    case SendAction.Send:
                        SendInternal(itemToSend.Message, itemToSend.Callback);
                        break;
                    case SendAction.Publish:
                        PublishInternal(itemToSend.Message as IEvent);
                        break;
                    case SendAction.Route:
                        RouteInternal(itemToSend.Message, itemToSend.PeerName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public IBlockableUntilCompletion Send(ICommand message, ICompletionCallback callback = null)
        {
            var nonNullCallback = callback ?? new DefaultCompletionCallback();
            _itemsToSend.Add(new ItemToSend { Callback = nonNullCallback, Message = message, SendAction = SendAction.Send });
            return nonNullCallback;
        }

        public void Publish(IEvent message)
        {
            _itemsToSend.Add(new ItemToSend { Message = message, SendAction = SendAction.Publish });
        }

        public void Route(IMessage message, string peerName)
        {
            _itemsToSend.Add(new ItemToSend { Message = message, PeerName = peerName, SendAction = SendAction.Route });
        }

        public void SendInternal(IMessage message, ICompletionCallback callback)
        {
            var concernedSubscriptions = _peerManager.GetSubscriptionsForMessageType(message.GetType().FullName).Where(x => x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(message));
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingBusMessage sendingMessage = GetTransportMessage(message);
            _callbackRepository.RegisterCallback(sendingMessage.MessageIdentity, callback);
            sendingStrat.Send(sendingMessage, concernedSubscriptions);
        }


        private ISendingBusMessage GetTransportMessage(IMessage message)
        {
            return new SendingBusMessage(message.GetType().FullName, Guid.NewGuid(), Serializer.Serialize(message));
        }

        public void PublishInternal(IMessage message)
        {
            var concernedSubscriptions = _peerManager.GetSubscriptionsForMessageType(message.GetType().FullName).Where(x => x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(message));
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingBusMessage sendingMessage = GetTransportMessage(message);
            sendingStrat.Publish(sendingMessage, concernedSubscriptions);
        }

        public void RouteInternal(IMessage message, string peerName)
        {
            var subscription = _peerManager.GetPeerSubscriptionFor(message.GetType().FullName, peerName);
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            ISendingBusMessage sendingMessage = GetTransportMessage(message);
            sendingStrat.Send(sendingMessage, new List<IMessageSubscription> { subscription });
        }

        public void Dispose()
        {
            _strategyFactory.Dispose();
            _itemsToSend.CompleteAdding();
        }
    }
}