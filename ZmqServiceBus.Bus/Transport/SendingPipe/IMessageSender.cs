using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface IMessageSender
    {
        IBlockableUntilCompletion Send(ICommand command, ICompletionCallback callback = null);
        void Publish(IEvent message);
        void Route(IMessage message, string peerName);
    }
}