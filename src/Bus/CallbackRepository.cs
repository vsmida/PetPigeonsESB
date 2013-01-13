using System;
using System.Collections.Concurrent;

namespace Bus
{
    public class CallbackRepository : ICallbackRepository
    {
        private readonly ConcurrentDictionary<Guid, ICompletionCallback> _completionCallbacksById = new ConcurrentDictionary<Guid, ICompletionCallback>();

        public void RegisterCallback(Guid messageId, ICompletionCallback callback)
        {
            _completionCallbacksById.TryAdd(messageId, callback);
        }

        public ICompletionCallback GetCallback(Guid messageId)
        {
            ICompletionCallback callback;
            _completionCallbacksById.TryGetValue(messageId, out callback);
            return callback;
        }

        public void RemoveCallback(Guid messageId)
        {
            ICompletionCallback callback;
            _completionCallbacksById.TryRemove(messageId, out callback);
        }
    }
}