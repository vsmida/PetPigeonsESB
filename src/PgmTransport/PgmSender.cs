using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Shared;
using log4net;
using System.Linq;

namespace PgmTransport
{
    public interface IPgmSender : IDisposable
    {
        void Send(IPEndPoint endpoint, byte[] buffer);
        void SendAsync(IPEndPoint endpoint, byte[] buffer);
        void DisconnectEndpoint(IPEndPoint endpoint);
    }


    public abstract class SocketSender : IDisposable
    {
        private struct FrameToSend
        {
            public ArraySegment<byte> Frame { get; set; }
            public IPEndPoint Endpoint { get; set; }
        }

        private readonly ConcurrentDictionary<IPEndPoint, Socket> _endPointToSockets = new ConcurrentDictionary<IPEndPoint, Socket>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs(), 10000);
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmSender));
        protected int _buffersSize = 2048;
        private ConcurrentQueue<FrameToSend> _frameQueue;
        private ConcurrentQueue<FrameToSend> _switchQueue;


        private Dictionary<IPEndPoint, List<ArraySegment<byte>>> _stuffToSend = new Dictionary<IPEndPoint, List<ArraySegment<byte>>>();
        private Dictionary<IPEndPoint, int> _stuffToSendSize = new Dictionary<IPEndPoint, int>();
        private Thread _iothread;

        protected SocketSender()
        {
            _frameQueue = new ConcurrentQueue<FrameToSend>();
            _switchQueue = new ConcurrentQueue<FrameToSend>();
            CreateIOThread();
        }

        public void Send(IPEndPoint endpoint, byte[] buffer)
        {
            var socket = GetSocket(endpoint);
            if (socket == null)
                return;

            var necessaryBuffers = Math.Ceiling(((double)buffer.Length) / _buffersSize);
            byte[] lengthInBytes = BitConverter.GetBytes(buffer.Length);

            var sentBytes = socket.Send(lengthInBytes, 0, lengthInBytes.Length, SocketFlags.None);
            CheckError(sentBytes, lengthInBytes.Length, socket);

            for (int i = 0; i < necessaryBuffers; i++)
            {
                var length = Math.Min(buffer.Length - (i) * _buffersSize, _buffersSize);
                sentBytes = socket.Send(buffer, i * 1024, length, SocketFlags.None);
                CheckError(sentBytes, length, socket);
            }

        }

        private void CreateIOThread()
        {
            _iothread = new Thread(() =>
                                       {
                                           try
                                           {

                                               var spinWait = default(SpinWait);
                                               while (true)
                                               {
                                                   ConcurrentQueue<FrameToSend> queue = null;
                                                   queue = _frameQueue;
                                                   Interlocked.Exchange(ref _frameQueue, _switchQueue);
                                                   if (queue != null)
                                                   {
                                                       _stuffToSend.Clear();
                                                       _stuffToSendSize.Clear();
                                                       var count = queue.Count;
                                                       for (int i = 0; i < count; i++)
                                                       {
                                                           FrameToSend frameToSend;
                                                           queue.TryDequeue(out frameToSend);
                                                           List<ArraySegment<byte>> list;
                                                           if (!_stuffToSend.TryGetValue(frameToSend.Endpoint, out list))
                                                           {
                                                               _stuffToSend.Add(frameToSend.Endpoint, new List<ArraySegment<byte>>());
                                                           }
                                                           _stuffToSend[frameToSend.Endpoint].Add(new ArraySegment<byte>(BitConverter.GetBytes(frameToSend.Frame.Count), 0, 4)); //header
                                                           _stuffToSend[frameToSend.Endpoint].Add(frameToSend.Frame); //data

                                                           if (!_stuffToSendSize.ContainsKey(frameToSend.Endpoint))
                                                               _stuffToSendSize.Add(frameToSend.Endpoint, 1);
                                                           else
                                                               _stuffToSendSize[frameToSend.Endpoint] +=
                                                                   frameToSend.Frame.Count;

                                                           if (_stuffToSendSize[frameToSend.Endpoint] >= 600000)//for PGM, to cleanup
                                                           {
                                                               FlushDataForEndpoint(frameToSend);
                                                           }

                                                       }

                                                       foreach (var pair in _stuffToSend)
                                                       {
                                                           if (pair.Value.Count == 0)
                                                               continue;
                                                           var socket = GetSocket(pair.Key);
                                                           if (socket == null)
                                                               continue;

                                                           var sentBytes = socket.Send(pair.Value, SocketFlags.None);
                                                           CheckError(sentBytes, pair.Value.Sum(x => x.Count), socket);


                                                       }


                                                   }
                                                   _switchQueue = queue;
                                                   spinWait.SpinOnce();
                                               }
                                           }
                                           catch (ThreadAbortException exception)
                                           {
                                               _logger.Error(exception);
                                               Thread.ResetAbort();

                                           }

                                       });

            _iothread.Start();
        }

        private void FlushDataForEndpoint(FrameToSend frameToSend)
        {
            var socket = GetSocket(frameToSend.Endpoint);
            var sentBytes = socket.Send(_stuffToSend[frameToSend.Endpoint], SocketFlags.None);
            CheckError(sentBytes, _stuffToSend[frameToSend.Endpoint].Sum(x => x.Count), socket);
            _stuffToSendSize[frameToSend.Endpoint] = 0;
            _stuffToSend[frameToSend.Endpoint] = new List<ArraySegment<byte>>();
        }

        public void SendAsync2(IPEndPoint endpoint, byte[] buffer)
        {
            var frameClass = new FrameToSend
                                 {Frame = new ArraySegment<byte>(buffer, 0, buffer.Length), Endpoint = endpoint};

            _frameQueue.Enqueue(frameClass);
        }

        private void CheckError(int sentBytes, int length, Socket socket)
        {
            if (sentBytes != length)
                _logger.Warn("Not all bytes sent");
            var socketError = (SocketError)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
            if (socketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error on send :  {0}", socketError);
            }
        }

        private Socket GetSocket(IPEndPoint endpoint)
        {
            Socket socket;
            if (!_endPointToSockets.TryGetValue(endpoint, out socket))
            {
                _logger.Info(string.Format("Creating send socket for endpoint {0}", endpoint));
                socket = CreateSocket(endpoint);
                if (socket == null)
                    return null;
                _endPointToSockets.TryAdd(endpoint, socket);
            }
            return socket;
        }

        public void SendAsync(IPEndPoint endpoint, byte[] buffer)
        {
            var socket = GetSocket(endpoint);
            if (socket == null)
                return;


            var necessaryBuffers = Math.Ceiling(((double)buffer.Length) / _buffersSize);
            byte[] lengthInBytes = BitConverter.GetBytes(buffer.Length);

            var firstEventArgs = _eventArgsPool.GetItem();
            firstEventArgs.Completed += OnSendCompleted;

            var bufferList = firstEventArgs.BufferList;
            if (bufferList == null)
                bufferList = new List<ArraySegment<byte>>();
            else
                bufferList.Clear();
            bufferList.Add(new ArraySegment<byte>(lengthInBytes));

            for (int i = 0; i < necessaryBuffers; i++)
            {
                var length = Math.Min(buffer.Length - (i) * _buffersSize, _buffersSize);
                bufferList.Add(new ArraySegment<byte>(buffer, i * _buffersSize, length));

            }
            firstEventArgs.BufferList = bufferList;
            if (!socket.SendAsync(firstEventArgs))
                OnSendCompleted(socket, firstEventArgs);
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error on send :  {0}", e.SocketError);
            }

            if (e.BytesTransferred != e.BufferList.Sum(x => x.Count))
            {
                _logger.Warn("could not send all bytes");
            }
            e.Completed -= OnSendCompleted;
            _eventArgsPool.PutBackItem(e);
        }

        public void DisconnectEndpoint(IPEndPoint endpoint)
        {
            Socket socket;
            if (!_endPointToSockets.TryGetValue(endpoint, out socket))
                return;
            socket.Close();
            socket.Dispose();

        }

        public void Dispose()
        {
            foreach (var socket in _endPointToSockets.Values)
            {
                socket.Dispose();
            }

            _iothread.Abort();
        }


        protected abstract Socket CreateSocket(IPEndPoint endpoint);

    }

    public class PgmSender : SocketSender
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmSender));

        protected override Socket CreateSocket(IPEndPoint endpoint)
        {
            try
            {
                var sendingSocket = new PgmSocket();
                sendingSocket.SendBufferSize = 1024 * 1024;
                sendingSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                sendingSocket.SetSocketOption(PgmSocket.PGM_LEVEL, (SocketOptionName)PgmConstants.RM_SEND_WINDOW_ADV_RATE, 20);
                var window = new _RM_SEND_WINDOW();
                window.RateKbitsPerSec = 0;
                window.WindowSizeInBytes = 100 * 1000 * 1000;
                window.WindowSizeInMSecs = 100;
                sendingSocket.SetSendWindow(window);

                sendingSocket.EnableGigabit();
                var tt2 = sendingSocket.GetSendWindow();
                _logger.Info(string.Format("connecting socket to {0}", endpoint));
                sendingSocket.Connect(endpoint);
                _logger.Info(string.Format("finished connecting socket to {0}", endpoint));

                return sendingSocket;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return null;
            }

        }

    }

    public class TcpSender : SocketSender
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(TcpSender));

        protected override Socket CreateSocket(IPEndPoint endpoint)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendBufferSize = 1024 * 1024;
                socket.Connect(endpoint);
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