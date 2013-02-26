using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared;
using log4net;

namespace PgmTransport
{
    public interface IPgmReceiver : IDisposable
    {
        void ListenToEndpoint(IPEndPoint endpoint);
        void StopListeningTo(IPEndPoint endpoint);
    }


    public abstract class SocketReceiver : IDisposable
    {
        private readonly ConcurrentDictionary<IPEndPoint, Socket> _endPointToAcceptSockets = new ConcurrentDictionary<IPEndPoint, Socket>();
        private readonly ConcurrentDictionary<Socket, FrameAccumulator> _receivingSockets = new ConcurrentDictionary<Socket, FrameAccumulator>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs(), 10000);
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmReceiver));
        private readonly Pool<byte[]> _bufferPool;
        public Action<IPEndPoint, Stream> OnMessageReceived = delegate { };
        private bool _disposing = false;
        private object _disposeLock = new object();


        public SocketReceiver()
        {
            _bufferPool = new Pool<byte[]>(() => new byte[16384], 2000);
        }

        public void ListenToEndpoint(IPEndPoint endpoint)
        {
            Socket socket;
            if (!_endPointToAcceptSockets.TryGetValue(endpoint, out socket))
            {
                socket = CreateAcceptSocket(endpoint);
                _endPointToAcceptSockets[endpoint] = socket;
            }

            var acceptEventArgs = _eventArgsPool.GetItem();
            acceptEventArgs.UserToken = endpoint;
            acceptEventArgs.Completed += OnAccept;
            if (!socket.AcceptAsync(acceptEventArgs))
                OnAccept(socket, acceptEventArgs);
        }

        public void StopListeningTo(IPEndPoint endpoint)
        {
            Socket socket;
            if (!_endPointToAcceptSockets.TryGetValue(endpoint, out socket))
                return;
            socket.Dispose();
            _endPointToAcceptSockets.TryRemove(endpoint, out socket);
        }

        protected abstract Socket CreateAcceptSocket(IPEndPoint endpoint);

        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            var receiveSocket = e.AcceptSocket;
            receiveSocket.ReceiveBufferSize = 1024 * 1024;
            if (e.SocketError != SocketError.Success)
            {
                lock (_disposeLock)
                {
                    if (_disposing)
                        return;
                }
                _logger.ErrorFormat("Error : {0}", e.SocketError);
                Debugger.Break();
                socket.Close();
                socket.Dispose();
                var endpoint = (IPEndPoint)e.UserToken;
                Socket s;
                _endPointToAcceptSockets.TryRemove(endpoint, out s);
                ListenToEndpoint((IPEndPoint)e.UserToken);
                return;
            }
            _logger.InfoFormat("AcceptingSocket from: {0}", e.AcceptSocket.RemoteEndPoint);

            _receivingSockets[receiveSocket] = new FrameAccumulator();
            var receiveEventArgs = _eventArgsPool.GetItem();
            receiveEventArgs.UserToken = socket.LocalEndPoint;
            receiveEventArgs.Completed += OnReceive;
            if (receiveEventArgs.Buffer == null)
            {
                byte[] buffer = _bufferPool.GetItem();
                receiveEventArgs.SetBuffer(buffer, 0, buffer.Length);
            }

            if (!receiveSocket.ReceiveAsync(receiveEventArgs))
                OnReceive(receiveSocket, receiveEventArgs);

            e.AcceptSocket = null;
            if (!socket.AcceptAsync(e))
            {
                OnAccept(socket, _eventArgsPool.GetItem());
                _eventArgsPool.PutBackItem(e);

            }

        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            var socket = (Socket)sender;
            if (CheckError(socket, e))
                return;
            DoReceive(socket, e);
            while (!socket.ReceiveAsync(e))
            {
                if (CheckError(socket, e))
                    return;
                DoReceive(socket, e);

            }

        }

        private void DoReceive(Socket socket, SocketAsyncEventArgs e)
        {
            {
                var messageReady = _receivingSockets[socket].AddFrame(new Frame(e.Buffer, 0, e.BytesTransferred));
                if (messageReady)
                {
                    var messages = _receivingSockets[socket].GetMessages();
                    while (messages.Count > 0)
                        OnMessageReceived((IPEndPoint)e.UserToken, messages.Dequeue());
                }
            }

        }

        private bool CheckError(Socket socket, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                _logger.ErrorFormat("Error : {0}", e.SocketError);
                socket.Dispose();
                return true;
            }
            return false;
        }


        public void Dispose()
        {
            lock (_disposeLock)
            {
                _disposing = true;
                foreach (var socket in _receivingSockets.Keys.Concat(_endPointToAcceptSockets.Values))
                {
                    socket.Dispose();
                }
            }

        }
    }

    public class PgmReceiver : SocketReceiver
    {

        protected override Socket CreateAcceptSocket(IPEndPoint endpoint)
        {
            var socket = new PgmSocket();
            socket.Bind(endpoint);
            socket.SetPgmOption(PgmConstants.RM_HIGH_SPEED_INTRANET_OPT, PgmSocket.ConvertStructToBytes(true));
            socket.EnableGigabit();
            socket.Listen(5);
            return socket;
        }
        
    }

    public class TcpReceiver : SocketReceiver
    {

        protected override Socket CreateAcceptSocket(IPEndPoint endpoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endpoint);
            socket.Listen(5);
            return socket;
        }

    }
}