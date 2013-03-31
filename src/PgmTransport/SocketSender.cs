using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<long, Action> _timers = new Dictionary<long, Action>();
        private readonly ILog _logger = LogManager.GetLogger(typeof(SocketSender));
        private int _buffersSize = 2048;
        private ConcurrentQueue<FrameToSend> _frameQueue;
        private Stopwatch _watch = new Stopwatch();

        private Dictionary<IPEndPoint, List<ArraySegment<byte>>> _stuffToSend = new Dictionary<IPEndPoint, List<ArraySegment<byte>>>();
        private Dictionary<IPEndPoint, List<ArraySegment<byte>>> _failedStuffToSend = new Dictionary<IPEndPoint, List<ArraySegment<byte>>>();
        private Dictionary<IPEndPoint, int> _stuffToSendSize = new Dictionary<IPEndPoint, int>();
        private Thread _iothread;

        protected SocketSender()
        {
            _frameQueue = new ConcurrentQueue<FrameToSend>();
            _watch.Start();
            CreateIOThread();
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

                                                   ExecuteElapsedTimers();
                                                   {
                                                       var count = _frameQueue.Count;
                                                       for (int i = 0; i < count; i++)
                                                       {

                                                           FrameToSend frameToSend;
                                                           _frameQueue.TryDequeue(out frameToSend);
                                                           var currentEndpoint = frameToSend.Endpoint;
                                                           var stuffToSendForFrameEndpoint = _stuffToSend.GetOrCreateNew(currentEndpoint);

                                                           int size = 0;
                                                           _stuffToSendSize.TryGetValue(currentEndpoint, out size);
                                                           if (size >= 60000)//for PGM or udp, to cleanup
                                                           {
                                                               SendData(currentEndpoint, stuffToSendForFrameEndpoint);
                                                               _stuffToSend[currentEndpoint].Clear();
                                                               _stuffToSendSize[currentEndpoint] = 0;
                                                           }

                                                           AddFrameDataToAggregatedSocketData(stuffToSendForFrameEndpoint, frameToSend, currentEndpoint);

                                                       }

                                                       foreach (var pair in _stuffToSend)
                                                       {
                                                           if (pair.Value.Count == 0)
                                                               continue;
                                                           SendData(pair.Key, pair.Value);
                                                       }
                                                   }

                                                   ResetFrameAggregationDictionaries();


                                                   spinWait.SpinOnce();
                                               }
                                           }
                                           catch (ThreadAbortException exception)
                                           {
                                               //_logger.Error(exception);
                                               Thread.ResetAbort();

                                           }

                                       });

            _iothread.Start();
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

        private void AddFrameDataToAggregatedSocketData(List<ArraySegment<byte>> stuffToSendForFrameEndpoint, FrameToSend frameToSend, IPEndPoint currentEndpoint)
        {
            stuffToSendForFrameEndpoint.Add(new ArraySegment<byte>(BitConverter.GetBytes(frameToSend.Frame.Count), 0, 4));
            //header
            stuffToSendForFrameEndpoint.Add(frameToSend.Frame); //data

            if (!_stuffToSendSize.ContainsKey(currentEndpoint))
                _stuffToSendSize.Add(currentEndpoint, 0);

            _stuffToSendSize[currentEndpoint] += frameToSend.Frame.Count + 4;
        }

        private void SendData(IPEndPoint endpoint, List<ArraySegment<byte>> data)
        {
            int sentBytes = 0;
            Socket socket = null;
            try
            {
                socket = GetSocket(endpoint);
                if (socket == null) //socket has been previously disconnected or cannot be created somehow, circuit breaker dont do anything
                {
                    SaveUnsentData(endpoint, socket, sentBytes, data);
                    return;
                }

                sentBytes = socket.Send(data, SocketFlags.None);
                CheckError(sentBytes, data.Sum(x => x.Count), socket);

                _stuffToSendSize[endpoint] = 0;
                _stuffToSend[endpoint].Clear();
            }

            catch (SocketException e)
            {
                _logger.Error(string.Format("Error on send {0}", e.Message));
                if (socket != null)
                {
                    socket.Disconnect(false);
                    socket.Dispose();
                    socket = null;
                }

                _endPointToSockets[endpoint] = null;
                _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(endpoint));
                SaveUnsentData(endpoint, socket, sentBytes, data);
            }
        }

        private void ResetFrameAggregationDictionaries()
        {
            foreach (var key in _stuffToSend.Keys.ToList())
            {
                List<ArraySegment<byte>> failedStuff = null;
                if (_failedStuffToSend.TryGetValue(key, out failedStuff))
                {
                    _stuffToSend[key] = failedStuff;
                    _stuffToSendSize[key] = failedStuff.Sum(x => x.Count);
                    _failedStuffToSend.Remove(key);
                }
                else
                {
                    _stuffToSend[key].Clear(); //key inflation due to new sockets
                    _stuffToSendSize[key] = 0;
                }
            }
        }

        private void SaveUnsentData(IPEndPoint currentEndpoint, Socket socket, int sentBytes, List<ArraySegment<byte>> stuffToSendForFrameEndpoint)
        {
            if (!_failedStuffToSend.ContainsKey(currentEndpoint))
                _failedStuffToSend.Add(currentEndpoint, new List<ArraySegment<byte>>());
            if (socket == null || sentBytes == 0)
            {
                _failedStuffToSend[currentEndpoint].AddRange(stuffToSendForFrameEndpoint);
            }
            else
            {
                int sentBeforeFail = 0;
                for (int j = 0; j < stuffToSendForFrameEndpoint.Sum(x => x.Count); j++)
                {
                    sentBeforeFail += stuffToSendForFrameEndpoint[j].Count;
                    if (sentBeforeFail > sentBytes)
                    {
                        _failedStuffToSend[currentEndpoint].AddRange(stuffToSendForFrameEndpoint.Skip(j));
                    }
                }
            }
        }

        public void Send(IPEndPoint endpoint, byte[] buffer)
        {
            var frameClass = new FrameToSend { Frame = new ArraySegment<byte>(buffer, 0, buffer.Length), Endpoint = endpoint };
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


        private Socket CreateSocketForEndpoint(IPEndPoint endpoint)
        {
            try
            {
                _logger.Info(string.Format("Creating send socket for endpoint {0}", endpoint));
                var socket = CreateSocket(endpoint);
                _endPointToSockets[endpoint] = socket;
                if (socket == null)
                {
                    _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(endpoint));
                    return null;
                }
                return socket;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Error when creating socket for endpoint {0}, mess = {1}", endpoint, e.Message));
                _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(endpoint));
                return null;
            }

        }

        private Socket GetSocket(IPEndPoint endpoint)
        {
            Socket socket;
            if (!_endPointToSockets.TryGetValue(endpoint, out socket))
            {
                socket = CreateSocketForEndpoint(endpoint);
            }

            return socket;
        }

        public void DisconnectEndpoint(IPEndPoint endpoint)
        {
            Socket socket;
            if (!_endPointToSockets.TryGetValue(endpoint, out socket))
                return;
            socket.Shutdown(SocketShutdown.Send);
            socket.Dispose(); //??

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
}