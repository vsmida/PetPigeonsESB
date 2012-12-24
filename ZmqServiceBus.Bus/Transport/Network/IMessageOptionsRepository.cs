using System;
using System.Collections.Concurrent;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;

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
        public event Action<MessageOptions> OptionsUpdated = delegate{};
        private readonly ConcurrentDictionary<string, MessageOptions> _options = new ConcurrentDictionary<string, MessageOptions>();
        
        public void RegisterOptions(MessageOptions options)
        {
            _options.AddOrUpdate(options.MessageType, options, (key, oldValue) => options);
            OptionsUpdated(options);
        }

        public MessageOptions GetOptionsFor(string messageType)
        {
            MessageOptions option;
            _options.TryGetValue(messageType, out option);
            return option;
        }
    }
}