namespace ZmqServiceBus.Transport
{
    public interface IAdressableMessage : ITransportMessage
    {
        string PeerId { get; }
    }
}