using System;
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

    public class PgmSender : IPgmSender
    {
        private readonly Dictionary<IPEndPoint, PgmSocket> _endPointToSockets = new Dictionary<IPEndPoint, PgmSocket>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs());
        private readonly ILog _logger = LogManager.GetLogger(typeof (PgmSender));

        public void Send(IPEndPoint endpoint, byte[] buffer)
        {
            var socket = GetSocket(endpoint);

            var necessaryBuffers = Math.Ceiling((buffer.Length + 4)/1024m);
            byte[] lengthInBytes = BitConverter.GetBytes(buffer.Length);

            socket.Send(lengthInBytes, 0, lengthInBytes.Length, SocketFlags.None);

            for (int i = 0; i < necessaryBuffers; i++)
            {
                var length = Math.Min(buffer.Length - (i) * 1024, 1024);
                socket.Send(buffer, i * 1024,length , SocketFlags.None);

            }
            
        }

        public void SendAsync(IPEndPoint endpoint, byte[] buffer)
        {
            var socket = GetSocket(endpoint);

            var necessaryBuffers = Math.Ceiling((buffer.Length + 4) / 1024m);
            byte[] lengthInBytes = BitConverter.GetBytes(buffer.Length);

            var firstEventArgs = _eventArgsPool.GetItem();
            firstEventArgs.Completed += OnSendCompleted;
            firstEventArgs.SetBuffer(lengthInBytes, 0, lengthInBytes.Length);

            if(!socket.SendAsync(firstEventArgs))
                OnSendCompleted(socket, firstEventArgs);

            for (int i = 0; i < necessaryBuffers; i++)
            {
                var length = Math.Min(buffer.Length - (i) * 1024, 1024);
                var eventArgs = _eventArgsPool.GetItem();
                eventArgs.Completed += OnSendCompleted;
                eventArgs.SetBuffer(buffer, i * 1024, length);
                if(!socket.SendAsync(eventArgs))
                    OnSendCompleted(socket, eventArgs);
            }

            
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if(e.SocketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error on send :  {0}", e.SocketError);
            }

            if(e.BytesTransferred != e.Buffer.Length)
            {
                _logger.Warn("could not send all bytes");
            }
            e.Completed -= OnSendCompleted;
            _eventArgsPool.PutBackItem(e);
        }

        private PgmSocket GetSocket(IPEndPoint endpoint)
        {
            PgmSocket socket;
            if (!_endPointToSockets.TryGetValue(endpoint, out socket))
            {
                socket = CreateSocket(endpoint);
                _endPointToSockets.Add(endpoint, socket);
            }
            return socket;
        }

        private PgmSocket CreateSocket(IPEndPoint endpoint)
        {
            var sendingSocket = new PgmSocket();
            sendingSocket.SendBufferSize = 1024 * 1024;
            sendingSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

            var window = new _RM_SEND_WINDOW();
            window.RateKbitsPerSec = 1000000;
            window.WindowSizeInMSecs = 0;
            window.WindowSizeInBytes = 10000000 * 20;

            sendingSocket.SetSendWindow(window);

            sendingSocket.EnableGigabit();

            sendingSocket.Connect(endpoint);
            return sendingSocket;
        }

        public void DisconnectEndpoint(IPEndPoint endpoint)
        {
            PgmSocket socket;
            if (!_endPointToSockets.TryGetValue(endpoint, out socket))
                return;
            socket.Dispose();

        }

        public void Dispose()
        {
            foreach (var socket in _endPointToSockets.Values)
            {
                socket.Dispose();
            }
        }
    }
}