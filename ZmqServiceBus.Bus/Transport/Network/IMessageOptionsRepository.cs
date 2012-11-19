using System;
using Shared;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IMessageOptionsRepository
    {
        event Action<MessageOptions> OptionsUpdated;
        void RegisterOptions(MessageOptions options);
        MessageOptions GetOptionsFor(string messageType);
    }

    public class MessageOptionsRepository : IMessageOptionsRepository
    {
        public event Action<MessageOptions> OptionsUpdated;
        public void RegisterOptions(MessageOptions options)
        {
            throw new NotImplementedException();
        }

        public MessageOptions GetOptionsFor(string messageType)
        {
            throw new NotImplementedException();
        }
    }
}