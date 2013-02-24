using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Shared;
using log4net;

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

        private readonly ConcurrentDictionary<IPEndPoint, Socket> _endPointToSockets = new ConcurrentDictionary<IPEndPoint, Socket>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs());
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmSender));
        protected int _buffersSize = 2048;


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
            firstEventArgs.SetBuffer(lengthInBytes, 0, lengthInBytes.Length);

            if (!socket.SendAsync(firstEventArgs))
                OnSendCompleted(socket, firstEventArgs);

            for (int i = 0; i < necessaryBuffers; i++)
            {
                var length = Math.Min(buffer.Length - (i) * _buffersSize, _buffersSize);
                var eventArgs = _eventArgsPool.GetItem();
                eventArgs.Completed += OnSendCompleted;
                eventArgs.SetBuffer(buffer, i * _buffersSize, length);

                if (!socket.SendAsync(eventArgs))
                    OnSendCompleted(socket, eventArgs);
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error on send :  {0}", e.SocketError);
            }

            if (e.BytesTransferred != e.Buffer.Length)
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
                sendingSocket.SetSocketOption(PgmSocket.PGM_LEVEL,(SocketOptionName)PgmConstants.RM_SEND_WINDOW_ADV_RATE,20);
                var window = new _RM_SEND_WINDOW();
                window.RateKbitsPerSec = 0;
                window.WindowSizeInBytes = 10 * 1000 * 1000 ;
                window.WindowSizeInMSecs = 100;
                sendingSocket.SetSendWindow(window);

                sendingSocket.EnableGigabit();
                var tt2 = sendingSocket.GetSendWindow();
                _logger.Info(string.Format("connecting socket to {0}", endpoint));
                sendingSocket.Connect(endpoint);
                _logger.Info(string.Format("finished connecting socket to {0}", endpoint));

                return sendingSocket;
            }
            catch(Exception e)
            {
                _logger.Error(e);
                return null;
            }

        }
        
    }

    public class TcpSender : SocketSender
    {
        protected override Socket CreateSocket(IPEndPoint endpoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendBufferSize = 1024 * 1024;
            socket.Connect(endpoint);
            return socket;
        }
    }
}