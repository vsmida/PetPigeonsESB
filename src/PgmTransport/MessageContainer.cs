using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PgmTransport
{
    internal class MessageContainer
    {
        private readonly ConcurrentQueue<ArraySegment<byte>> _frames = new ConcurrentQueue<ArraySegment<byte>>();
        private readonly Queue<ArraySegment<byte>> _failedFrames = new Queue<ArraySegment<byte>>();

        internal void InsertMessage(ArraySegment<byte> message)
        {
            _frames.Enqueue(message);
        }

        internal bool TryGetNextMessage(out ArraySegment<byte> message)
        {
            if (_failedFrames.Count > 0)
            {
                message = _failedFrames.Dequeue();
                return true;
            }
            return _frames.TryDequeue(out message);
        }

        internal void PutBackFailedMessage(ArraySegment<byte> unsentMessage)
        {
            _failedFrames.Enqueue(unsentMessage);
        }

        internal int Count
        {
            get { return _failedFrames.Count + _frames.Count; }
        }
    }
}