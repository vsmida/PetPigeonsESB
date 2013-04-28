using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;

namespace PgmTransport
{
    public abstract class TransportPipe
    {
        private readonly ConcurrentQueue<ArraySegment<byte>> _frames = new ConcurrentQueue<ArraySegment<byte>>();
        private readonly int _highWaterMark;
        private readonly HighWaterMarkBehavior _highWaterMarkBehavior; //use polymorphism instead?
        
        public TransportPipe(int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior)
        {
            _highWaterMarkBehavior = highWaterMarkBehavior;
            _highWaterMark = highWaterMark;
        }

        public bool Send(ArraySegment<byte> data)
        {
            if (_frames.Count < _highWaterMark)
                _frames.Enqueue(data);
            else
            { 
                switch (_highWaterMarkBehavior)
                {
                    case HighWaterMarkBehavior.Drop:
                        return false;
                    case HighWaterMarkBehavior.Block:
                        {
                            var wait = new SpinWait();
                            while (_frames.Count >= _highWaterMark)
                            {
                                wait.SpinOnce();
                            }
                            _frames.Enqueue(data);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return true;
        }

        public abstract Socket CreateSocket();
        }

    public class TcpTransportPipe : TransportPipe
    {
        private IPEndPoint _endPoint;
        private readonly ILog _logger = LogManager.GetLogger(typeof(TcpTransportPipe));

        public TcpTransportPipe(IPEndPoint endPoint, int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior) : base(highWaterMark, highWaterMarkBehavior)
        {
            _endPoint = endPoint;
        }

        public override Socket CreateSocket()
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendBufferSize = 1024 * 1024;
                socket.Connect(_endPoint);
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