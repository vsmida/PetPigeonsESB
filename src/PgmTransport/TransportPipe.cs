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

        internal TransportPipe(int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior, IPEndPoint endPoint, SendingTransport transport,IMessageContainer messageContainer, int sendingThreadNumber = 0)
        {
            _transport = transport;
            _highWaterMarkBehavior = highWaterMarkBehavior;
            EndPoint = endPoint;
            _highWaterMark = highWaterMark;
            MessageContainerConcurrentQueue = messageContainer;
            transport.AttachToIoThread(this, sendingThreadNumber);
        }

        public bool Send(ArraySegment<byte> data, bool dontWait = false)
        {
            if (MessageContainerConcurrentQueue.Count < _highWaterMark)
                MessageContainerConcurrentQueue.InsertMessage(data);
            else
            {
                switch (_highWaterMarkBehavior)
                {
                    case HighWaterMarkBehavior.Drop:
                        return false;
                    case HighWaterMarkBehavior.Block:
                        {
                            if(MessageContainerConcurrentQueue.Count >= _highWaterMark)
                            {
                                if (dontWait)
                                    return false;
                                var wait = new SpinWait();
                                while (MessageContainerConcurrentQueue.Count >= _highWaterMark)
                                {
                                    wait.SpinOnce();
                                }
                                MessageContainerConcurrentQueue.InsertMessage(data); 
                            }
                            else
                            {
                                MessageContainerConcurrentQueue.InsertMessage(data);                                
                            }
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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