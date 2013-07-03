using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PgmTransport
{
    public abstract class TransportPipe : IDisposable
    {
        internal readonly IMessageContainer MessageContainerConcurrentQueue;
        private readonly int _highWaterMark;
        private readonly HighWaterMarkBehavior _highWaterMarkBehavior; //use polymorphism instead?
        public readonly IPEndPoint EndPoint;
        private readonly SendingTransport _transport;

        public abstract int MaximumBatchSize { get; }
        public abstract int MaximumBatchCount { get; }

        internal TransportPipe(int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior, IPEndPoint endPoint, SendingTransport transport, int sendingThreadNumber = 0)
        {
            _transport = transport;
            _highWaterMarkBehavior = highWaterMarkBehavior;
            EndPoint = endPoint;
            _highWaterMark = highWaterMark;
            MessageContainerConcurrentQueue = new MessageContainerConcurrentQueue(MaximumBatchCount, MaximumBatchSize);
            transport.AttachToIoThread(this, sendingThreadNumber);
        }

        public bool Send(ArraySegment<byte> data, bool dontWait = false)
        {
       
            switch (_highWaterMarkBehavior)
            {
                case HighWaterMarkBehavior.Drop:
                    if (MessageContainerConcurrentQueue.Count > _highWaterMark)
                        return false;
                    else
                        MessageContainerConcurrentQueue.InsertMessage(data);
                    break;

                case HighWaterMarkBehavior.Block:
                    {
                        MessageContainerConcurrentQueue.InsertMessage(data);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();

            }
            return true;
        }

        public abstract Socket CreateSocket();
        public void Dispose()
        {
            _transport.DetachFromIoThread(this);
        }
    }
}