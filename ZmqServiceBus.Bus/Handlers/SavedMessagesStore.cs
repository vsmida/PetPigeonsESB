using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Handlers
{
    public interface ISavedMessagesStore
    {
        void SaveMessage(ShadowMessageCommand shadowMessage);
        void RemoveMessage(string peer,WireTransportType transportType, Guid messageId);
    }

    public class SavedMessagesStore : ISavedMessagesStore
    {
        private class PeerMessageQueue
        {
            public readonly string PeerName;
            private Dictionary<WireTransportType, Queue<ShadowMessageCommand>> _messagesByEndpoint = new Dictionary<WireTransportType, Queue<ShadowMessageCommand>>();

            public PeerMessageQueue(string peerName)
            {
                PeerName = peerName;
            }

            public Queue<ShadowMessageCommand> this[WireTransportType key]
            {
                get
                {
                    Queue<ShadowMessageCommand> queue;
                    if(!_messagesByEndpoint.TryGetValue(key, out queue))
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

            queue[MessageContext.OriginatingTransportType.Value].Enqueue(shadowMessage);
        }

        public void RemoveMessage(string peer,WireTransportType transportType, Guid messageId)
        {
            PeerMessageQueue peerQueue;
             if (!_savedMessages.TryGetValue(peer, out peerQueue) || peerQueue[transportType].Count == 0)
             {
                 //argh nothing, restart?
                 Debugger.Break();
             }
            var item = peerQueue[transportType].Dequeue();
            if(item.Message.MessageIdentity != messageId)
            {
                //argh, missing messages?
                Debugger.Break();
            }


        }
    }
}