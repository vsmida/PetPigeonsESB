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
        private readonly IDataSender _dataSender;
        private readonly Thread _sendingThread;

        public MessageSender(IMessageOptionsRepository messageOptionsRepository, IReliabilityStrategyFactory strategyFactory, ICallbackRepository callbackRepository, IPeerManager peerManager, IDataSender dataSender)
        {
            _messageOptionsRepository = messageOptionsRepository;
            _strategyFactory = strategyFactory;
            _callbackRepository = callbackRepository;
            _peerManager = peerManager;
            _dataSender = dataSender;

          //  _sendingThread = new Thread(MainSendingLoop);
          //  _sendingThread.Start();
        }

        //private void MainSendingLoop()
        //{
        //    foreach (var itemToSend in _itemsToSend.GetConsumingEnumerable())
        //    {
        //        switch (itemToSend.SendAction)
        //        {
        //            case SendAction.Send:
        //                SendInternal(itemToSend.Message, itemToSend.Callback);
        //                break;
        //            case SendAction.Publish:
        //                PublishInternal(itemToSend.Message as IEvent);
        //                break;
        //            case SendAction.Route:
        //                RouteInternal(itemToSend.Message, itemToSend.PeerName);
        //                break;
        //            default:
        //                throw new ArgumentOutOfRangeException();
        //        }
        //    }
        //}
        
        public ICompletionCallback Send(ICommand message, ICompletionCallback callback = null)
        {
            var nonNullCallback = callback ?? new DefaultCompletionCallback();
            SendInternal(message, nonNullCallback);
      //      _itemsToSend.Add(new ItemToSend { Callback = nonNullCallback, Message = message, SendAction = SendAction.Send });
            return nonNullCallback;
        }

        
        public void Publish(IEvent message)
        {
            _itemsToSend.Add(new ItemToSend { Message = message, SendAction = SendAction.Publish });
        }

        public ICompletionCallback Route(IMessage message, string peerName)
        {
            return RouteInternal(message, peerName);
           // _itemsToSend.Add(new ItemToSend { Message = message, PeerName = peerName, SendAction = SendAction.Route });
        }

        public void SendInternal(IMessage message, ICompletionCallback callback)
        {
            var concernedSubscriptions = _peerManager.GetSubscriptionsForMessageType(message.GetType().FullName).Where(x => x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(message));
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            sendingStrat.ReliabilityAchieved += callback.Release;
            var messagesToSend = sendingStrat.Send(message, concernedSubscriptions);
            foreach (var mess in messagesToSend)
            {
                _callbackRepository.RegisterCallback(mess.MessageIdentity, callback);
                _dataSender.SendMessage(mess);
            }
            
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
            var sendingStrat = _strategyFactory.GetSendingStrategy(_messageOptionsRepository.GetOptionsFor(message.GetType().FullName));
            sendingStrat.ReliabilityAchieved += callback.Release;
            var messagesToSend = sendingStrat.Send(message, new[]{subscription});
            foreach (var mess in messagesToSend)
            {
                _callbackRepository.RegisterCallback(mess.MessageIdentity, callback);
                _dataSender.SendMessage(mess);
            }

            return callback;
        }

        public void Dispose()
        {
            _itemsToSend.CompleteAdding();
            _dataSender.Dispose();
            //_sendingThread.Join();
        }
    }
}