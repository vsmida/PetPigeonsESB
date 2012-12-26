using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface IMessageSender : IDisposable
    {
        ICompletionCallback Send(ICommand command, ICompletionCallback callback = null);
        void Publish(IEvent message);
        void Route(IMessage message, string peerName);
    }
}