using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface IMessageSender
    {
        void Send(ICommand command);
        void Publish(IEvent message);
        void Route(IMessage message, string peerName);
    }
}