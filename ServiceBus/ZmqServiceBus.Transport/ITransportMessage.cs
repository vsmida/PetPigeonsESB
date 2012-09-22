namespace ZmqServiceBus.Transport
{
    public interface ITransportMessage
    {
        string SenderIdentity { get; }
        string MessageType { get; }
        byte[] Data { get; }
    }

    public class TransportMessage : ITransportMessage
    {
        public string SenderIdentity { get; private set; }
        public string MessageType { get; private set; }
        public byte[] Data { get; private set; }

        public TransportMessage(string senderIdentity, string messageType, byte[] data)
        {
            SenderIdentity = senderIdentity;
            MessageType = messageType;
            Data = data;
        }
    }
}