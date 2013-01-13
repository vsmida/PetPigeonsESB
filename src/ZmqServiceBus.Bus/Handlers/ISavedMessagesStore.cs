using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Handlers
{
    public interface ISavedMessagesStore
    {
        void SaveMessage(ShadowMessageCommand shadowMessage);
        void RemoveMessage(ShadowCompletionMessage completionMessage);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, WireTransportType transportType, int maxCount);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, int maxCount);
    }
}