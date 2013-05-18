using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Shared;
using log4net;

namespace PgmTransport
{
    internal class SendingThread : IDisposable
    {
        private readonly Thread _thread;
        private readonly ConcurrentBag<TransportPipe> _transportPipes = new ConcurrentBag<TransportPipe>();
        private readonly ConcurrentDictionary<TransportPipe, Socket> _endPointToSockets = new ConcurrentDictionary<TransportPipe, Socket>();
        private readonly Dictionary<IPEndPoint, List<ArraySegment<byte>>> _stuffToSend = new Dictionary<IPEndPoint, List<ArraySegment<byte>>>();
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly Dictionary<long, Action> _timers = new Dictionary<long, Action>();
        private readonly ILog _logger = LogManager.GetLogger(typeof(SendingThread));
        private readonly ConcurrentDictionary<TransportPipe, AutoResetEvent> _pipesToBeRemoved = new ConcurrentDictionary<TransportPipe, AutoResetEvent>();


        internal SendingThread()
        {
            _thread = new Thread(SendingLoop) { IsBackground = true };
            _thread.Start();
        }

        internal void Attach(TransportPipe pipe)
        {
            _transportPipes.Add(pipe);
        }

        private void ExecuteElapsedTimers()
        {
            foreach (var ticks in _timers.Keys.ToList())
            {
                if (_watch.ElapsedTicks >= ticks)
                {
                    _timers[ticks]();
                    _timers.Remove(ticks);
                }

            }
        }

        private int AddFrameDataToAggregatedSocketData(List<ArraySegment<byte>> stuffToSendForFrameEndpoint, ArraySegment<byte> frameToSend)
        {
            stuffToSendForFrameEndpoint.Add(new ArraySegment<byte>(BitConverter.GetBytes(frameToSend.Count), 0, 4));//header
            stuffToSendForFrameEndpoint.Add(frameToSend); //data
            return frameToSend.Count + 4;
        }

        private void SendingLoop()
        {
            try
            {
                var spinWait = default(SpinWait);
                while (true)
                {
                    ExecuteElapsedTimers();
                    {
                        foreach (var pipe in _transportPipes)
                        {
                            var stuffToSendForFrameEndpoint = _stuffToSend.GetOrCreateNew(pipe.EndPoint);

                            var messageCount = pipe.MessageContainer.Count;
                            if (messageCount == 0)
                                continue;
                            var sizeToSend = 0; //todo : use for pgm or avoiding sending too much data at once.
                            for (int i = 0; i < messageCount; i++)
                            {
                                ArraySegment<byte> message;
                                pipe.MessageContainer.TryGetNextMessage(out message); // should always work, only one dequeuer
                                sizeToSend += AddFrameDataToAggregatedSocketData(stuffToSendForFrameEndpoint, message);
                            }
                            SendData(pipe, stuffToSendForFrameEndpoint);
                            stuffToSendForFrameEndpoint.Clear();
                            RemovePipeIfNeeded(pipe);

                        }
                    }
                    spinWait.SpinOnce();
                }
            }
            catch (ThreadAbortException)
            {
                //_logger.Error(exception);
                Thread.ResetAbort();
            }
        }

        private void RemovePipeIfNeeded(TransportPipe pipe)
        {
            if (_pipesToBeRemoved.ContainsKey(pipe))
            {
                TransportPipe toRemove;
                _transportPipes.TryTake(out toRemove);
                Socket socket;
                _endPointToSockets.TryRemove(pipe, out socket);
                if (socket != null)
                    socket.Dispose();
                AutoResetEvent waitHandle;
                _pipesToBeRemoved.TryRemove(toRemove, out waitHandle);


                waitHandle.Set();
            }
        }

        private Socket GetSocket(TransportPipe pipe)
        {
            Socket socket;
            if (!_endPointToSockets.TryGetValue(pipe, out socket))
            {
                socket = CreateSocketForEndpoint(pipe);
            }

            return socket;
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


        private void SendData(TransportPipe pipe, List<ArraySegment<byte>> data)
        {
            int sentBytes = 0;
            Socket socket = null;
            try
            {
                socket = GetSocket(pipe);
                if (socket == null) //socket has been previously disconnected or cannot be created somehow, circuit breaker dont do anything
                {
                    SaveUnsentData(pipe, data);
                    return;
                }

                sentBytes = socket.Send(data, SocketFlags.None);
                CheckError(sentBytes, data.Sum(x => x.Count), socket);

            }

            catch (SocketException e)
            {
                _logger.Error(string.Format("Error on send {0}", e.Message));
                if (socket != null)
                {
                    socket.Disconnect(false);
                    socket.Dispose();
                }

                _endPointToSockets[pipe] = null;
                _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(pipe));
                SaveUnsentData(pipe, data);
            }

        }

        private Socket CreateSocketForEndpoint(TransportPipe pipe)
        {
            try
            {
                _logger.Info(string.Format("Creating send socket for endpoint {0}", pipe.EndPoint));
                var socket = pipe.CreateSocket();
                if (!_endPointToSockets.ContainsKey(pipe)) //dont add again in case it was detached
                    //todo: race condition here, could be detaching while this is being called in the main thread; could lock on dictionary here and in detach.
                    _endPointToSockets[pipe] = socket;
                if (socket == null)
                {
                    _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(pipe));
                    return null;
                }
                return socket;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Error when creating socket for endpoint {0}, mess = {1}", pipe.EndPoint, e.Message));
                _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(pipe));
                return null;
            }

        }


        private static void SaveUnsentData(TransportPipe pipe, List<ArraySegment<byte>> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (i % 2 == 1) //eliminate headers
                    pipe.MessageContainer.PutBackFailedMessage(data[i]);
            }
        }

        public void Dispose()
        {
            _thread.Abort();
            foreach (var socket in _endPointToSockets.Values)
            {
                socket.Dispose();
            }
        }

        public void Detach(TransportPipe pipe)
        {
            var waitHandle = new AutoResetEvent(false);
            _pipesToBeRemoved.TryAdd(pipe, waitHandle);
            waitHandle.WaitOne();
        }
    }
}