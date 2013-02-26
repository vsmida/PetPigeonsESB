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
        private class FrameClass
        {
            public Frame Frame { get; set; }
            public IPEndPoint Endpoint { get; set; }
        }

        private readonly ConcurrentDictionary<IPEndPoint, Socket> _endPointToSockets = new ConcurrentDictionary<IPEndPoint, Socket>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs(), 10000);
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmSender));
        protected int _buffersSize = 2048;
        private ConcurrentQueue<FrameClass> _frameQueue;
        private ConcurrentQueue<FrameClass> _switchQueue;
        private int _messagesPending = 0;
        private object _pulse = new object();

        private Dictionary<IPEndPoint, List<ArraySegment<byte>>> _stuffToSend = new Dictionary<IPEndPoint, List<ArraySegment<byte>>>();
        private Dictionary<IPEndPoint, int> _stuffToSendSize = new Dictionary<IPEndPoint, int>();
        private Thread _iothread;

        protected SocketSender()
        {
            _frameQueue = new ConcurrentQueue<FrameClass>();
            _switchQueue = new ConcurrentQueue<FrameClass>();
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
                                                   ConcurrentQueue<FrameClass> queue = null;
                                                   Thread.MemoryBarrier();
                                                 //  lock (_pulse)
                                                 //  {
                                               //        if (_frameQueue.Count == 0)
                                                //           Monitor.Wait(_pulse);
                                                       queue = _frameQueue;
                                                       _frameQueue = _switchQueue;

                                               //    }
                                                   if (queue != null)
                                                   {
                                                       _stuffToSend.Clear();
                                                       _stuffToSendSize.Clear();
                                                       var count = queue.Count;
                                                       for (int i = 0; i < count; i++)
                                                       {
                                                           FrameClass frameClass;
                                                           queue.TryDequeue(out frameClass);
                                                           List<ArraySegment<byte>> list;
                                                           if (!_stuffToSend.TryGetValue(frameClass.Endpoint, out list))
                                                           {
                                                               _stuffToSend.Add(frameClass.Endpoint,
                                                                                new List<ArraySegment<byte>>());
                                                           }
                                                           _stuffToSend[frameClass.Endpoint].Add(
                                                               new ArraySegment<byte>(
                                                                   BitConverter.GetBytes(frameClass.Frame.Count),
                                                                   0,
                                                                   4));
                                                           _stuffToSend[frameClass.Endpoint].Add(
                                                               new ArraySegment<byte>(frameClass.Frame.Buffer,
                                                                                      frameClass.Frame.Offset,
                                                                                      frameClass.Frame.Count));
                                                           if (!_stuffToSendSize.ContainsKey(frameClass.Endpoint))
                                                               _stuffToSendSize.Add(frameClass.Endpoint, 1);
                                                           else
                                                               _stuffToSendSize[frameClass.Endpoint] +=
                                                                   frameClass.Frame.Count;
                                                           if (_stuffToSendSize[frameClass.Endpoint] >= 600000)
                                                           {
                                                               var socket = GetSocket(frameClass.Endpoint);
                                                               if (socket == null)
                                                                   return;

                                                               var sentBytes =
                                                                   socket.Send(_stuffToSend[frameClass.Endpoint],
                                                                               SocketFlags.None);
                                                               CheckError(sentBytes,
                                                                          _stuffToSend[frameClass.Endpoint].Sum(
                                                                              x => x.Count),
                                                                          socket);
                                                               _stuffToSendSize[frameClass.Endpoint] = 0;
                                                               _stuffToSend[frameClass.Endpoint] =
                                                                   new List<ArraySegment<byte>>();
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

                                                           //foreach (var arraySegment in pair.Value)
                                                           //{
                                                           //    var necessaryBuffers = Math.Ceiling(((double)buffer.Length) / _buffersSize);
                                                           //    byte[] lengthInBytes = BitConverter.GetBytes(buffer.Length);

                                                           //    var sentBytes = socket.Send(lengthInBytes, 0, lengthInBytes.Length, SocketFlags.None);
                                                           //    CheckError(sentBytes, lengthInBytes.Length, socket);

                                                           //    for (int i = 0; i < necessaryBuffers; i++)
                                                           //    {
                                                           //        var length = Math.Min(buffer.Length - (i) * _buffersSize, _buffersSize);
                                                           //        sentBytes = socket.Send(buffer, i * 1024, length, SocketFlags.None);
                                                           //        CheckError(sentBytes, length, socket);
                                                           //    }
                                                           //}

                                                       }


                                                   }
                                                   _switchQueue = queue;

                                                   spinWait.SpinOnce();
                                               }
                                           }
                                           catch(ThreadAbortException exception)
                                           {
                                               _logger.Error(exception);
                                               Thread.ResetAbort();
                                               
                                           }

                                       });

            _iothread.Start();
        }

        public void SendAsync2(IPEndPoint endpoint, byte[] buffer)
        {
            var frameClass = new FrameClass();
            frameClass.Frame = new Frame(buffer, 0, buffer.Length);
            frameClass.Endpoint = endpoint;
         //   lock(_pulse)
         //   {
                _frameQueue.Enqueue(frameClass);     
            //    if(_frameQueue.Count == 1)
           //         Monitor.Pulse(_pulse);

         //   }
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
        private readonly ILog _logger = LogManager.GetLogger(typeof (TcpSender));

        protected override Socket CreateSocket(IPEndPoint endpoint)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendBufferSize = 1024 * 1024;
                socket.Connect(endpoint);
                return socket;
            }
            catch(Exception e)
            {
                _logger.Error(e);
                return null;
            }

        }
    }
}