using System;
using System.Collections.Generic;
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

    public class PgmReceiver : IPgmReceiver
    {
        private readonly Dictionary<IPEndPoint, PgmSocket> _endPointToAcceptSockets = new Dictionary<IPEndPoint, PgmSocket>();
        private readonly Dictionary<Socket, FrameAccumulator> _receivingSockets = new Dictionary<Socket, FrameAccumulator>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs());
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmReceiver));
        private readonly BufferManager _bufferManager;
        public Action<byte[]> OnMessageReceived = delegate { };


        public PgmReceiver()
        {
            _bufferManager = new BufferManager(1024, 1024);
        }

        public void ListenToEndpoint(IPEndPoint endpoint)
        {
            PgmSocket socket;
            if (!_endPointToAcceptSockets.TryGetValue(endpoint, out socket))
            {
                socket = CreateAcceptSocket(endpoint);
                _endPointToAcceptSockets.Add(endpoint, socket);
            }

            var acceptEventArgs = _eventArgsPool.GetItem();
            acceptEventArgs.Completed += OnAccept;
            if(socket.AcceptAsync(acceptEventArgs))
                OnAccept(socket, acceptEventArgs);
        }

        public void StopListeningTo(IPEndPoint endpoint)
        {
           PgmSocket socket;
           if (!_endPointToAcceptSockets.TryGetValue(endpoint, out socket))
               return;
            socket.Dispose();
            _endPointToAcceptSockets.Remove(endpoint);
        }

        private static PgmSocket CreateAcceptSocket(IPEndPoint endpoint)
        {
            var socket = new PgmSocket();
            socket.Bind(endpoint);
            socket.EnableGigabit();
            socket.Listen(5);
            return socket;
        }

        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            var receiveSocket = e.AcceptSocket;
            if (e.SocketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error : {0}", e.SocketError);
                return;
            }
            _logger.InfoFormat("AcceptingSocket from: {0}", e.AcceptSocket.RemoteEndPoint);

            _receivingSockets[receiveSocket] = new FrameAccumulator();
            var receiveEventArgs = _eventArgsPool.GetItem();
            receiveEventArgs.Completed += OnReceive;
            byte[] buffer = _bufferManager.GetBuffer();
            receiveEventArgs.SetBuffer(buffer, 0, buffer.Length);
            if(receiveSocket.ReceiveAsync(receiveEventArgs))
                OnReceive(receiveSocket, receiveEventArgs);



            e.AcceptSocket = null;
            if (socket.AcceptAsync(e))
                OnAccept(socket, e);
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            if (e.SocketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error : {0}", e.SocketError);
                socket.Dispose();
                return;
            }

            if (e.BytesTransferred == 4)
            {
                var length = BitConverter.ToInt32(e.Buffer, 0);
                _receivingSockets[socket].SetLength(length);

            }
            else
            {
                var messageReady = _receivingSockets[socket].AddFrame(new Frame
                                                       {
                                                           Buffer = e.Buffer,
                                                           Count = e.BytesTransferred,
                                                           Offset = 0
                                                       });
                if (messageReady)
                {
                    var message = _receivingSockets[socket].GetMessage();
                    OnMessageReceived(message);
                }


            }
            if(socket.ReceiveAsync(e))
                OnReceive(socket, e);
        }


        public void Dispose()
        {
            foreach (var socket in _receivingSockets.Keys.Concat(_endPointToAcceptSockets.Values))
            {
                socket.Dispose();
            }
        }

    }
}