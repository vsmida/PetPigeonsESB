using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PgmTransport
{
    internal interface IMessageContainer
    {
        void InsertMessage(ArraySegment<byte> message);
        bool TryGetNextMessage(out ArraySegment<byte> message);
        void PutBackFailedMessage(ArraySegment<byte> unsentMessage);
        int Count { get; }
    }

    internal class MessageContainerConcurrentQueue : IMessageContainer
    {
        private readonly ConcurrentQueue<ArraySegment<byte>> _frames = new ConcurrentQueue<ArraySegment<byte>>();
        private readonly Queue<ArraySegment<byte>> _failedFrames = new Queue<ArraySegment<byte>>();

        public void InsertMessage(ArraySegment<byte> message)
        {
            _frames.Enqueue(message);
        }

        public bool TryGetNextMessage(out ArraySegment<byte> message)
        {
            if (_failedFrames.Count > 0)
            {
                message = _failedFrames.Dequeue();
                return true;
            }
            return _frames.TryDequeue(out message);
        }

        public void PutBackFailedMessage(ArraySegment<byte> unsentMessage)
        {
            _failedFrames.Enqueue(unsentMessage);
        }

        public int Count
        {
            get { return _failedFrames.Count + _frames.Count; }
        }
    }
}