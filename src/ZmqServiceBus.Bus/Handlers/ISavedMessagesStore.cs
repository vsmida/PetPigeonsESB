using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Handlers
{
    public interface ISavedMessagesStore
    {
        void SaveMessage(ShadowMessageCommand shadowMessage);
        void RemoveMessage(string peer, WireTransportType transportType, Guid messageId);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, WireTransportType transportType, int maxCount);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, int maxCount);
    }
}