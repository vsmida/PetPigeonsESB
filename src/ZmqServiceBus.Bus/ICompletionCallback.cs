using System;
using ZmqServiceBus.Bus.InfrastructureMessages;

namespace ZmqServiceBus.Bus
{
    public interface ICompletionCallback : IBlockableUntilCompletion
    {
        void RegisterCallback(Action<CompletionAcknowledgementMessage> onCompletion);
        void ExecuteCallback(CompletionAcknowledgementMessage message);
    }
}