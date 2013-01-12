using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class WireSendingMessage
    {
        public MessageWireData MessageData { get; private set; }
        public IEndpoint Endpoint { get; private set; }

        public WireSendingMessage(MessageWireData messageData, IEndpoint endpoint)
        {
            this.MessageData = messageData;
            Endpoint = endpoint;
        }
    }
}