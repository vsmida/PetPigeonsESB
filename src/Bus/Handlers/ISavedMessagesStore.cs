using System.Collections.Generic;
using Bus.InfrastructureMessages.Shadowing;
using Bus.Transport.Network;

namespace Bus.Handlers
{
    public interface ISavedMessagesStore
    {
        void SaveMessage(ShadowMessageCommand shadowMessage);
        void RemoveMessage(ShadowCompletionMessage completionMessage);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, IEndpoint transportType, int maxCount);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(string peer, int? maxCount);
    }
}