using System;
using Bus.InfrastructureMessages;

namespace Bus
{
    public interface ICompletionCallback : IBlockableUntilCompletion
    {
        void RegisterCallback(Action<CompletionAcknowledgementMessage> onCompletion);
        void ExecuteCallback(CompletionAcknowledgementMessage message);
    }
}