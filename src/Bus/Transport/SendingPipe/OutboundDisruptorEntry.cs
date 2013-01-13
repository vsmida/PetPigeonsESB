using System.Collections.Generic;
using Bus.MessageInterfaces;

namespace Bus.Transport.SendingPipe
{
    public class OutboundDisruptorEntry
    {
        public MessageTargetHandlerData MessageTargetHandlerData = new MessageTargetHandlerData();
        public NetworkSenderData NetworkSenderData = new NetworkSenderData();
    }

    public class MessageTargetHandlerData
    {
        public IMessage Message;
        public bool IsAcknowledgement;
        public ICompletionCallback Callback;
        public string TargetPeer;
    }

    public class NetworkSenderData
    {
        public readonly List<WireSendingMessage> WireMessages = new List<WireSendingMessage>(2);        
    }
}