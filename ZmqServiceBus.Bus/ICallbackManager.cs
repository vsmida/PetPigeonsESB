using System;

namespace ZmqServiceBus.Bus
{
    public interface ICallbackManager
    {
        void RegisterCallback(Guid messageId, ICompletionCallback callback);
        ICompletionCallback GetCallback(Guid messageId);
    }
}