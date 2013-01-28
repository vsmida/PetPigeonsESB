using System.Collections.Generic;
using Bus.MessageInterfaces;

namespace Bus.Transport.SendingPipe
{
    class OutboundDisruptorEntry
    {
        public MessageTargetHandlerData MessageTargetHandlerData = new MessageTargetHandlerData();
        public NetworkSenderData NetworkSenderData = new NetworkSenderData();
    }

    class MessageTargetHandlerData
    {
        public IMessage Message;
        public bool IsAcknowledgement;
        public ICompletionCallback Callback;
        public string TargetPeer;
    }

    class NetworkSenderData
    {
        public readonly List<WireSendingMessage> WireMessages = new List<WireSendingMessage>(2);
        public IBusEventProcessorCommand Command;
    }
}