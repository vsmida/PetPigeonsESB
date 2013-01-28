using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bus.Dispatch;
using Bus.InfrastructureMessages.Shadowing;
using Bus.Transport.Network;

namespace Bus.Handlers
{
    class SavedMessagesStore : ISavedMessagesStore
    {
        private class PeerMessageQueue
        {
            public readonly string PeerName;
            private readonly Dictionary<IEndpoint, Queue<ShadowMessageCommand>> _messagesByEndpoint = new Dictionary<IEndpoint, Queue<ShadowMessageCommand>>();
            private Queue<ShadowMessageCommand> _messagesGlobal = new Queue<ShadowMessageCommand>();
            private Dictionary<Guid, ShadowCompletionMessage> _acksReceivedBeforeMessages = new Dictionary<Guid, ShadowCompletionMessage>();

            public PeerMessageQueue(string peerName)
            {
                PeerName = peerName;
            }


            public Queue<ShadowMessageCommand> GlobalQueue { get { return _messagesGlobal; } set { _messagesGlobal = value; } }
            public Dictionary<Guid, ShadowCompletionMessage> OutOfOrderAcks { get { return _acksReceivedBeforeMessages; } set { _acksReceivedBeforeMessages = value; } }

            public Queue<ShadowMessageCommand> this[IEndpoint key]
            {
                get
                {
                    Queue<ShadowMessageCommand> queue;
                    if (!_messagesByEndpoint.TryGetValue(key, out queue))
                    {
                        queue = new Queue<ShadowMessageCommand>();
                        _messagesByEndpoint[key] = queue;
                    }

                    return queue;
                }
                set { _messagesByEndpoint[key] = value; }
            }
        }

        private readonly Dictionary<string, PeerMessageQueue> _savedMessages = new Dictionary<string, PeerMessageQueue>();

        public void SaveMessage(ShadowMessageCommand shadowMessage)
        {
            PeerMessageQueue queue;
            if (!_savedMessages.TryGetValue(shadowMessage.PrimaryRecipient, out queue))
            {
                queue = new PeerMessageQueue(shadowMessage.PrimaryRecipient);
                _savedMessages[shadowMessage.PrimaryRecipient] = queue;
            }

            if (queue.OutOfOrderAcks.Count > 0 && queue.OutOfOrderAcks.ContainsKey(shadowMessage.Message.MessageIdentity))
            {
                Console.WriteLine(string.Format("removing out of order ack {0}", shadowMessage.Message.MessageIdentity));
                queue.OutOfOrderAcks.Remove(shadowMessage.Message.MessageIdentity);
                return;
            }
            queue[shadowMessage.TargetEndpoint].Enqueue(shadowMessage);
            queue.GlobalQueue.Enqueue(shadowMessage);
        }

        public void RemoveMessage(ShadowCompletionMessage message)
        {
            PeerMessageQueue peerQueue;
            if (!_savedMessages.TryGetValue(message.ToPeer, out peerQueue))
            {
                peerQueue = new PeerMessageQueue(message.ToPeer);
                _savedMessages.Add(message.ToPeer, peerQueue);

            }
            if (peerQueue[message.Endpoint].Count == 0 || peerQueue.GlobalQueue.Count == 0)
            {
                peerQueue.OutOfOrderAcks.Add(message.MessageId, message);
                return;
            }

            var item = peerQueue.GlobalQueue.Peek();
            if (item.Message.MessageIdentity != message.MessageId)
            {
                peerQueue.OutOfOrderAcks.Add(message.MessageId, message);
            }
            else
            {
                peerQueue.GlobalQueue.Dequeue();
                RemoveFromTransportQueue(message.Endpoint, message.MessageId, peerQueue);
            }

        }

        private static void RemoveFromTransportQueue(IEndpoint endpoint, Guid messageId, PeerMessageQueue peerQueue)
        {
            var item = peerQueue[endpoint].Dequeue();
            if (item.Message.MessageIdentity != messageId)
            {
                //argh, missing messages?
                Debugger.Break();
                Console.WriteLine("error when removing from queue");
            }
        }

        public IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, int? maxCount)
        {
            Queue<ShadowMessageCommand> newQueue = new Queue<ShadowMessageCommand>();
            PeerMessageQueue queue;
            if (!_savedMessages.TryGetValue(peer, out queue))
                yield break;
            int numberOfReturnedMessages = 0;
            while (queue.GlobalQueue.Count != 0 && (maxCount == null || numberOfReturnedMessages < maxCount))
            {
                var shadowMessageCommand = queue.GlobalQueue.Dequeue();
                newQueue.Enqueue(shadowMessageCommand);
                yield return shadowMessageCommand;
                numberOfReturnedMessages++;
            }

            while (queue.GlobalQueue.Count > 0)
            {
                newQueue.Enqueue(queue.GlobalQueue.Dequeue());
            }
            queue.GlobalQueue = newQueue;
        }

        public IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, IEndpoint endpoint, int maxCount)
        {
            PeerMessageQueue queue;
            if (!_savedMessages.TryGetValue(peer, out queue))
                yield break;
            int numberOfReturnedMessages = 0;
            while (queue[endpoint].Count != 0 && numberOfReturnedMessages < maxCount)
            {
                yield return queue[endpoint].Dequeue();
                numberOfReturnedMessages++;
            }
        }
    }
}