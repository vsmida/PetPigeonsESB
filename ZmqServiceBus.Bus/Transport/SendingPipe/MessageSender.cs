using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;
using System.Linq;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class MessageSender : IMessageSender
    {
        private readonly IMessageOptionsRepository _messageOptionsRepository;
        private readonly IReliabilityStrategyFactory _strategyFactory;
        private readonly ICallbackRepository _callbackRepository;
        private readonly IPeerManager _peerManager;
        private readonly IDataSender _dataSender;

        public MessageSender(IMessageOptionsRepository messageOptionsRepository, IReliabilityStrategyFactory strategyFactory, ICallbackRepository callbackRepository, IPeerManager peerManager, IDataSender dataSender)
        {
            _messageOptionsRepository = messageOptionsRepository;
            _strategyFactory = strategyFactory;
            _callbackRepository = callbackRepository;
            _peerManager = peerManager;
            _dataSender = dataSender;

        }

        public ICompletionCallback Send(ICommand message, ICompletionCallback callback = null)
        {
            var nonNullCallback = callback ?? new DefaultCompletionCallback();
            SendInternal(message, nonNullCallback);
            return nonNullCallback;
        }

        public void Publish(IEvent message)
        {
          //  _itemsToSend.Add(new ItemToSend { Message = message, SendAction = SendAction.Publish });
        }

        public ICompletionCallback Route(IMessage message, string peerName)
        {
            return RouteInternal(message, peerName);
           // _itemsToSend.Add(new ItemToSend { Message = message, PeerName = peerName, SendAction = SendAction.Route });
        }

        protected ISendingBusMessage GetTransportMessage(IMessage message, IEnumerable<IEndpoint> endpoints)
        {
            return new SendingBusMessage(message.GetType().FullName, Guid.NewGuid(), Serializer.Serialize(message), endpoints);
        }

        private void SendInternal(IMessage message, ICompletionCallback callback)
        {
            var concernedSubscriptions = _peerManager.GetSubscriptionsForMessageType(message.GetType().FullName).Where(x => x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(message));
            SendUsingSubscriptions(message, callback, concernedSubscriptions);
        }

        private void SendUsingSubscriptions(IMessage message, ICompletionCallback callback, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            var transportMessage = GetTransportMessage(message, concernedSubscriptions.Select(x => x.Endpoint));

            var waitForReliabilityToBeAchieved = new AutoResetEvent(false);
            var sendingStrat =
                _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            sendingStrat.ReliabilityAchieved += () => waitForReliabilityToBeAchieved.Set();
            sendingStrat.SetupCommandReliabilitySafeguards(transportMessage);

            _callbackRepository.RegisterCallback(transportMessage.MessageIdentity, callback);

            _dataSender.SendMessage(transportMessage);

            waitForReliabilityToBeAchieved.WaitOne();
        }

        public void PublishInternal(IMessage message)
        {
            var concernedSubscriptions = _peerManager.GetSubscriptionsForMessageType(message.GetType().FullName).Where(x => x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(message));
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
         //   ISendingBusMessage sendingMessage = GetTransportMessage(message);
         //   sendingStrat.Publish(sendingMessage, concernedSubscriptions);
        }

        public ICompletionCallback RouteInternal(IMessage message, string peerName)
        {
            var callback = new DefaultCompletionCallback();
            var subscription = _peerManager.GetPeerSubscriptionFor(message.GetType().FullName, peerName);
            SendUsingSubscriptions(message, callback, new[]{subscription});
            return callback;
        }

        public void Dispose()
        {
            _dataSender.Dispose();
            //_sendingThread.Join();
        }
    }
}