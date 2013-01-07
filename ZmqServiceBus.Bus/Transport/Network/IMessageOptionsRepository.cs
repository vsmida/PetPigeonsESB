using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Shared;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IMessageOptionsRepository
    {
        event Action<MessageOptions> OptionsUpdated;
        void RegisterOptions(MessageOptions options);
        MessageOptions GetOptionsFor(string messageType);
        List<MessageOptions> GetAllOptions();
        void InitializeOptions();
    }

    public class MessageOptionsRepository : IMessageOptionsRepository
    {
        public event Action<MessageOptions> OptionsUpdated = delegate{};
        private IAssemblyScanner _assemblyScanner;
        private readonly ConcurrentDictionary<string, MessageOptions> _options = new ConcurrentDictionary<string, MessageOptions>();

        public MessageOptionsRepository(IAssemblyScanner assemblyScanner)
        {
            _assemblyScanner = assemblyScanner;
        }

        public void RegisterOptions(MessageOptions options)
        {
            _options.AddOrUpdate(options.MessageType, options, (key, oldValue) => options);
            OptionsUpdated(options);
        }

        public MessageOptions GetOptionsFor(string messageType)
        {
            MessageOptions option;
            _options.TryGetValue(messageType, out option);
            return option ?? new MessageOptions(messageType, ReliabilityLevel.FireAndForget);
        }

        public List<MessageOptions> GetAllOptions()
        {
            return _options.Values.ToList();
        }

        public void InitializeOptions()
        {
            var options = _assemblyScanner.FindMessagesInfosInAssemblies();
            foreach (var info in options)
            {
                RegisterOptions(new MessageOptions(info.Key.FullName, info.Value));
            }
        }
    }
}