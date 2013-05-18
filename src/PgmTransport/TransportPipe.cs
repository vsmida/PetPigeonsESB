using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;

namespace PgmTransport
{
    public abstract class TransportPipe : IDisposable
    {
        internal readonly MessageContainer MessageContainer = new MessageContainer();
        private readonly int _highWaterMark;
        private readonly HighWaterMarkBehavior _highWaterMarkBehavior; //use polymorphism instead?
        public readonly IPEndPoint EndPoint;
        private SendingTransport _transport;

        public TransportPipe(int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior, IPEndPoint endPoint, SendingTransport transport, int sendingThreadNumber = 0)
        {
            _transport = transport;
            _highWaterMarkBehavior = highWaterMarkBehavior;
            EndPoint = endPoint;
            _highWaterMark = highWaterMark;
            transport.AttachToIoThread(this, sendingThreadNumber);
        }

        public bool Send(ArraySegment<byte> data)
        {
            if (MessageContainer.Count < _highWaterMark)
                MessageContainer.InsertMessage(data);
            else
            {
                switch (_highWaterMarkBehavior)
                {
                    case HighWaterMarkBehavior.Drop:
                        return false;
                    case HighWaterMarkBehavior.Block:
                        {
                            var wait = new SpinWait();
                            while (MessageContainer.Count >= _highWaterMark)
                            {
                                wait.SpinOnce();
                            }
                            MessageContainer.InsertMessage(data);
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

    public class TcpTransportPipe : TransportPipe
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(TcpTransportPipe));


        public TcpTransportPipe(int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior, IPEndPoint endPoint, SendingTransport transport, int sendingThreadNumber = 0) : base(highWaterMark, highWaterMarkBehavior, endPoint, transport, sendingThreadNumber)
        {
        }

        public override Socket CreateSocket()
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendBufferSize = 1024 * 1024;
                socket.Connect(EndPoint);
                return socket;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return null;
            }
        }
    }

}