﻿using System;
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
    public abstract class SocketReceiver : IDisposable
    {
        private readonly ConcurrentDictionary<IPEndPoint, Socket> _endPointToAcceptSockets = new ConcurrentDictionary<IPEndPoint, Socket>();
        private readonly Dictionary<IPEndPoint, List<Socket>> _endpointToReceiveSockets = new Dictionary<IPEndPoint, List<Socket>>();
        private readonly ConcurrentDictionary<Socket, FrameAccumulator> _receivingSockets = new ConcurrentDictionary<Socket, FrameAccumulator>();
        private readonly Pool<SocketAsyncEventArgs> _eventArgsPool = new Pool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs(), 10000);
        private readonly ILog _logger = LogManager.GetLogger(typeof(SocketReceiver));
        private readonly Pool<byte[]> _bufferPool;
        public readonly Dictionary<IPEndPoint, Action<Stream>> EventsForMessagesReceived = new Dictionary<IPEndPoint, Action<Stream>>();//todo : better
        private bool _disposing = false;
        private readonly object _disposeLock = new object();
        private const int _bufferLength = 1024*1024/3;


        public SocketReceiver()
        {
            _bufferPool = new Pool<byte[]>(() => new byte[_bufferLength], 100);
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

        public void RegisterCallback(IPEndPoint endpoint, Action<Stream> action)
        {
            lock (EventsForMessagesReceived)
            {
                Action<Stream> previousaction;
                if (!EventsForMessagesReceived.TryGetValue(endpoint, out previousaction))
                {
                    EventsForMessagesReceived[endpoint] = action;
                }
                else
                {
                    EventsForMessagesReceived[endpoint] += action;
                    EventsForMessagesReceived[endpoint] -= DummyEvent; //remove from list;

                }
            }

        }

        public void UnRegisterCallback(IPEndPoint endpoint, Action<Stream> action)
        {
            lock (EventsForMessagesReceived)
            {
                EventsForMessagesReceived[endpoint] -= action;
            }
        }
        public void StopListeningTo(IPEndPoint endpoint)
        {

            lock (_endpointToReceiveSockets)
            {
                List<Socket> socketsForEndpoint;
                if (_endpointToReceiveSockets.TryGetValue(endpoint, out socketsForEndpoint))
                {
                    foreach (var sock in socketsForEndpoint)
                    {
                        sock.Shutdown(SocketShutdown.Send);
                    }
                    socketsForEndpoint.Clear();
                }

            }
            Socket socket;
            if (!_endPointToAcceptSockets.TryGetValue(endpoint, out socket))
                return;
            socket.Close();
            _endPointToAcceptSockets.TryRemove(endpoint, out socket);
            lock (EventsForMessagesReceived)
                EventsForMessagesReceived.Remove(endpoint);
        }

        protected abstract Socket CreateAcceptSocket(IPEndPoint endpoint);

        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {


            var socket = sender as Socket;
            var receiveSocket = e.AcceptSocket;

            if (e.SocketError != SocketError.Success)
            {
                try
                {
                    lock (_disposeLock)
                    {
                        _logger.ErrorFormat("Error : {0}", e.SocketError);
                        if (_disposing)
                            return;
                        //  if (e.SocketError == SocketError.OperationAborted)
                        //      return;
                        socket.Disconnect(false);
                        socket.Dispose();
                        var endpoint = (IPEndPoint)e.UserToken;
                        Socket s;
                        _endPointToAcceptSockets.TryRemove(endpoint, out s);
                        ListenToEndpoint((IPEndPoint)e.UserToken);
                        return;
                    }
                }

                catch (ObjectDisposedException ex)
                {
                    //silence when socket was disposed by stoplistening
                    _logger.Error(string.Format("object disposed exception raised on accept socket {0}", ex.Source));
                    return;
                }
            }
            receiveSocket.ReceiveBufferSize = 1024 * 16;
            receiveSocket.NoDelay = true;
            _logger.InfoFormat("AcceptingSocket from: {0}", e.AcceptSocket.RemoteEndPoint);
            Console.WriteLine("AcceptingSocket from: {0}", e.AcceptSocket.RemoteEndPoint);

            lock (_endpointToReceiveSockets)
            {
                List<Socket> socketsForEndpoint;
                if (!_endpointToReceiveSockets.TryGetValue((IPEndPoint)e.UserToken, out socketsForEndpoint))
                {
                    socketsForEndpoint = new List<Socket>();
                    _endpointToReceiveSockets[(IPEndPoint)e.UserToken] = socketsForEndpoint;
                }
                socketsForEndpoint.Add(receiveSocket);
            }
            var frameAccumulator = new FrameAccumulator(_bufferLength);
            var localEndPoint = (IPEndPoint)socket.LocalEndPoint;
            Action<Stream> act;
            lock (EventsForMessagesReceived)
            {
                if (!EventsForMessagesReceived.ContainsKey(localEndPoint))
                    EventsForMessagesReceived[localEndPoint] = DummyEvent;
                act = EventsForMessagesReceived[localEndPoint];
            }
            frameAccumulator.MessageReceived += (s) => act(s);
            _receivingSockets[receiveSocket] = frameAccumulator;
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

        private void DummyEvent(Stream obj)
        {

        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            var socket = (Socket)sender;
            if (CheckError(socket, e))
                return;
            DoReceive(socket, e);
            try
            {
                while (!socket.ReceiveAsync(e))
                {
                    if (CheckError(socket, e))
                        return;
                    DoReceive(socket, e);

                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.Error(string.Format("receive socket for endpoint {0} was disposed", (IPEndPoint)e.UserToken));
            }


        }

        private void DoReceive(Socket socket, SocketAsyncEventArgs e)
        {
            _receivingSockets[socket].AddFrame(e.Buffer, e.Offset, e.BytesTransferred);
        }

        private bool CheckError(Socket socket, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.ConnectionReset)
            {
                _logger.ErrorFormat("Error : {0}", e.SocketError);
                return true;
            }

            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                _logger.ErrorFormat("Error : {0}", e.SocketError);
                lock (_endpointToReceiveSockets)
                {
                    socket.Dispose();

                    _endpointToReceiveSockets[e.UserToken as IPEndPoint].Remove(socket);
                    _bufferPool.PutBackItem(e.Buffer); //put back buffer in pool
                }
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