using System;

namespace Bus
{
    public interface ICallbackRepository
    {
        void RegisterCallback(Guid messageId, ICompletionCallback callback);
        ICompletionCallback GetCallback(Guid messageId);
        void RemoveCallback(Guid messageId);
    }
}