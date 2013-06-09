using System.Collections.Generic;
using Bus.InfrastructureMessages.Shadowing;
using Bus.Transport.Network;

namespace Bus.Handlers
{
    interface ISavedMessagesStore
    {
        void SaveMessage(ShadowMessageCommand shadowMessage);
        void RemoveMessage(ShadowCompletionMessage completionMessage);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(PeerId peer, IEndpoint transportType, int maxCount);
        IEnumerable<ShadowMessageCommand> GetFirstMessages(PeerId peer, int? maxCount);
    }
}