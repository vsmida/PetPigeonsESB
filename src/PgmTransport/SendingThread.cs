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
        private readonly List<TransportPipe> _transportPipes = new List<TransportPipe>();
        private readonly Dictionary<TransportPipe, Socket> _endPointToSockets = new Dictionary<TransportPipe, Socket>();
        private readonly Dictionary<IPEndPoint, List<ArraySegment<byte>>> _stuffToSend = new Dictionary<IPEndPoint, List<ArraySegment<byte>>>();
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly Dictionary<long, Action> _timers = new Dictionary<long, Action>();
        private readonly ConcurrentBag<Action> _commands = new ConcurrentBag<Action>();
        private readonly ILog _logger = LogManager.GetLogger(typeof(SendingThread));


        internal SendingThread()
        {
            _thread = new Thread(SendingLoop) { IsBackground = true };
            _thread.Start();
        }

        internal void Attach(TransportPipe pipe)
        {
            _commands.Add(() => _transportPipes.Add(pipe));
        }

        private void ExecuteElapsedTimers()
        {
            if (_timers.Count > 0)
                foreach (var ticks in _timers.Keys.ToList())
                {
                    if (_watch.ElapsedTicks >= ticks)
                    {
                        _timers[ticks]();
                        _timers.Remove(ticks);
                    }

                }
        }

        private int AddFrameDataToAggregatedSocketData(IList<ArraySegment<byte>> stuffToSendForFrameEndpoint, ArraySegment<byte> frameToSend)
        {
            var arraySegment = new ArraySegment<byte>(BitConverter.GetBytes(frameToSend.Count), 0, 4);
            stuffToSendForFrameEndpoint.Add(arraySegment);//header
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
                    ExecuteCommands();
                    ExecuteElapsedTimers();
                    {
                        foreach (var pipe in _transportPipes)
                        {
                            var stuffToSendForFrameEndpoint = _stuffToSend.GetOrCreateNew(pipe.EndPoint, () => new List<ArraySegment<byte>>(6002));

                            var messageCount = pipe.MessageContainerConcurrentQueue.Count;
                            if (messageCount == 0)
                                continue;
                            var sizeToSend = 0; //todo : use for pgm or avoiding sending too much data at once.
                            for (int i = 0; i < messageCount; i++)
                            {
                                ArraySegment<byte> message;
                                pipe.MessageContainerConcurrentQueue.TryGetNextMessage(out message); // should always work, only one dequeuer
                                sizeToSend += AddFrameDataToAggregatedSocketData(stuffToSendForFrameEndpoint, message);

                                if (sizeToSend >= pipe.MaximumBatchSize || stuffToSendForFrameEndpoint.Count > 4000)
                                {
                                    SendData(pipe, stuffToSendForFrameEndpoint, sizeToSend);
                                    stuffToSendForFrameEndpoint.Clear();
                                    sizeToSend = 0;
                                }
                            }
                            if (stuffToSendForFrameEndpoint.Count > 0)
                            {
                                SendData(pipe, stuffToSendForFrameEndpoint, sizeToSend);
                                stuffToSendForFrameEndpoint.Clear();
                            }

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
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private void ExecuteCommands()
        {
            for (int i = 0; i < _commands.Count; i++)
            {
                Action command;
                _commands.TryTake(out command);
                command();
            }
        }

        private void RemovePipe(TransportPipe pipe)
        {
            _transportPipes.Remove(pipe);
            Socket socket = _endPointToSockets[pipe];
            _endPointToSockets.Remove(pipe);
            if (socket != null)
                socket.Dispose();
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


        private void SendData(TransportPipe pipe, IList<ArraySegment<byte>> data, int dataSize)
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
                CheckError(sentBytes, dataSize, socket);

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


        private static void SaveUnsentData(TransportPipe pipe, IList<ArraySegment<byte>> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (i % 2 == 1) //eliminate headers
                    pipe.MessageContainerConcurrentQueue.PutBackFailedMessage(data[i]);
            }
        }

        public void Dispose()
        {
            _thread.Abort();
            _thread.Join();
            foreach (var socket in _endPointToSockets.Values)
            {
                socket.Dispose();
            }
        }

        public void Detach(TransportPipe pipe)
        {

            var waitHandle = new AutoResetEvent(false);
            _commands.Add(() =>
                              {
                                  RemovePipe(pipe);
                                  waitHandle.Set();
                              });
            waitHandle.WaitOne();
        }
    }
}