using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.Handlers;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{


    public class MessageTypeWindow
    {
        private int _currentFill = 0;
        private readonly int _windowSize;

        public MessageTypeWindow(int windowSize)
        {
            _windowSize = windowSize;
        }

        public bool CanStillSendMessage()
        {
            if (_currentFill == _windowSize)
                return false;
            _currentFill++;
            return true;
        }
    }
    
    public class SequenceNumberGenerator
    {
        public string PeerName;
        private int _sequenceNumber = -1;

        public int GetNextSequenceNumber()
        {
            _sequenceNumber++;
            return _sequenceNumber;
        }
    }



    internal class ReliabilityCoordinator
    {
        private readonly Dictionary<string, MessageTypeWindow> _currentWindowByMessageType = new Dictionary<string, MessageTypeWindow>();
        private readonly Dictionary<string, SequenceNumberGenerator> _sequenceNumberByPeer;
        private readonly IPeerManager _peerManager;

        public ReliabilityCoordinator(IPeerManager peerManager)
        {
            _peerManager = peerManager;
        }


        void Send(IMessage message, IEnumerable<MessageSubscription> validSubscriptions)
        {
        }

        void EnsureReliability(SendingBusMessage message)
        {

        }

        void RegisterAck(Guid messageId, string originatingPeer)
        {
         //   var messageType = _messageIdToMessageType[messageId];
       //     _currentWindowByMessageType[messageType].Window.Dequeue();
            //signal somehow
        }

    }






    internal class PersistStrategyCommands : SendingReliabilityStrategy
    {
        private readonly int _queueSize;
        private readonly IPersistenceSynchronizer _persistenceSynchronizer;
        private readonly Queue<SendingBusMessage> _windowQueue;
        private readonly object _locker = new object();

        public override void SetupReliabilitySafeguards(SendingBusMessage message)
        {

            lock (_locker)
            {
                _windowQueue.Enqueue(message);
                if (_windowQueue.Count == _queueSize)
                {
                    _persistenceSynchronizer.PersistMessages(_windowQueue);
                    _windowQueue.Clear();
                }
            }
            ReliabilityAchieved();
        }

        public override void RegisterAck(Guid messageId, string originatingPeer)
        {
            
        }

        //public override void RegisterAck(Guid messageId) // persistence done 
        //{
        //    lock (_locker)
        //    {
        //        if (_windowQueue.Count == 0)
        //        {
        //            //we persisted and we are getting acks?
        //            return;
        //        }
        //        var sendingMess = _windowQueue.Peek();
        //        if (sendingMess.MessageIdentity == messageId)
        //        {
        //            _windowQueue.Dequeue();
        //        }
        //        else
        //        {
        //            Debugger.Break();
        //            //missing message  do we need a seqNUm? Or acking of previously persisted messages
        //            //should resend previous messages? receiver should take care of seqNum
        //        }

        //    }
        //    ReliabilityAchieved();
        //}



        public override event Action ReliabilityAchieved = delegate { };

        //todo: special case when acknowledgement message. special message to broker to flush from queue? only for routing?
        public PersistStrategyCommands(int queueSize, IPersistenceSynchronizer persistenceSynchronizer)
        {
            _queueSize = queueSize;
            _persistenceSynchronizer = persistenceSynchronizer;
            _windowQueue = new Queue<SendingBusMessage>();
        }

        public PersistStrategyCommands(IPersistenceSynchronizer persistenceSynchronizer)
        {
            
        }
    }
}