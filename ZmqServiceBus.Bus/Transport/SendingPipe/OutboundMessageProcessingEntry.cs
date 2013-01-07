using System.Collections.Generic;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class OutboundMessageProcessingEntry
    {
        public IMessage Message;
        public ICompletionCallback Callback;
        public string TargetPeer;
        public List<WireSendingMessage> WireMessages = new List<WireSendingMessage>();
        public bool IsAcknowledgement;
    }
}