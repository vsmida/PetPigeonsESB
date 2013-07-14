using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Shared;
using log4net;

namespace PgmTransport
{
    internal class SendingThread : IDisposable
    {

        private class SendingPipeInfo
        {
            public readonly TransportPipe Pipe;
            public readonly SocketAsyncEventArgs EventArgs;

            public volatile bool IsSending;
            public Socket Socket;

            public SendingPipeInfo(TransportPipe pipe, SocketAsyncEventArgs eventArgs)
            {
                Pipe = pipe;
                EventArgs = eventArgs;
            }
        }

       // private readonly Thread _thread;
        private readonly List<SendingPipeInfo> _transportPipes = new List<SendingPipeInfo>();
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly Dictionary<long, Action> _timers = new Dictionary<long, Action>();
        private readonly ConcurrentBag<Action> _commands = new ConcurrentBag<Action>();
        private readonly ILog _logger = LogManager.GetLogger(typeof(SendingThread));
        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;


        internal SendingThread(TaskScheduler scheduler)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _task = Task.Factory.StartNew(SendingLoop, _cancellationTokenSource.Token, TaskCreationOptions.None, scheduler);
            //_thread = new Thread(SendingLoop) { IsBackground = true };

            _watch.Start();
            //_thread.Start();
        }

        internal void Attach(TransportPipe pipe)
        {
            _commands.Add(() =>
            {
                var socketAsyncEventArgs = new SocketAsyncEventArgs();
                var sendingPipeInfo = new SendingPipeInfo(pipe, socketAsyncEventArgs);
                socketAsyncEventArgs.UserToken = sendingPipeInfo;
                socketAsyncEventArgs.Completed += OnSendCompleted;
                _transportPipes.Add(sendingPipeInfo);
                CreateSocketForEndpoint(sendingPipeInfo);
            });
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)//multithreading here
        {
            var sendingPipeInfo = ((SendingPipeInfo)e.UserToken);
            var error = e.SocketError;
            if(error != SocketError.Success)
            {
                _commands.Add(() => { CreateSocketForEndpoint(sendingPipeInfo); });
                return;
            }
            sendingPipeInfo.Pipe.MessageContainerConcurrentQueue.FlushMessages(e.BufferList);
          //  sendingPipeInfo.HasSent = true;//commit

            IList<ArraySegment<byte>> data;
            var shouldSend = sendingPipeInfo.Pipe.MessageContainerConcurrentQueue.TryGetNextSegments(out data);
            if (shouldSend)
            {

                SendData(sendingPipeInfo, data, 0);//todo: restore size
            }
            else
            {
                sendingPipeInfo.IsSending = false;
            }
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

        private void SendingLoop()
        {
            try
            {
                var spinWait = default(SpinWait);
                bool sentSomething = false;
                while (true)
                {
                    ExecuteCommands();
                    ExecuteElapsedTimers();

                    foreach (var pipe in _transportPipes)
                    {
                        if (pipe.IsSending)//wait for completion
                            continue;
                        IList<ArraySegment<byte>> data;
                        var shouldSend = pipe.Pipe.MessageContainerConcurrentQueue.TryGetNextSegments(out data);
                        if (shouldSend)
                        {
                        
                            SendData(pipe, data, 0);//todo: restore size
                            sentSomething = true;
                        }
                    }
                    if (!sentSomething)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            return;
                        spinWait.SpinOnce();                        
                    }

                    sentSomething = false;
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
            var pipeInfo = _transportPipes.Single(x => x.Pipe == pipe);
            _transportPipes.Remove(pipeInfo);
            var socket = pipeInfo.Socket;
            if (socket != null)
                socket.Dispose();
        }


        private void CheckError(int sentBytes, int length, Socket socket)
        {
            // if (sentBytes != length)
            //     _logger.Warn("Not all bytes sent");
            var socketError = (SocketError)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
            if (socketError != SocketError.Success)
            {
                _logger.ErrorFormat("Error on send :  {0}", socketError);
            }
        }


        private void SendData(SendingPipeInfo pipe, IList<ArraySegment<byte>> data, int dataSize)
        {


            int sentBytes = 0;
            var socket = pipe.Socket;
            try
            {
                if (socket == null) //socket has been previously disconnected or cannot be created somehow, circuit breaker dont do anything
                {
                    //SaveUnsentData(pipe, data);
                    return;
                }
                pipe.EventArgs.BufferList = data;
                pipe.IsSending = true;
                if (!socket.SendAsync(pipe.EventArgs))
                    OnSendCompleted(null, pipe.EventArgs);
              //  CheckError(sentBytes, dataSize, socket);
                //     pipe.MessageContainerConcurrentQueue.FlushMessages(data);
            }

            catch (SocketException e)
            {
                _logger.Error(string.Format("Error on send {0}", e.Message));
                if (socket != null)
                {
                    socket.Disconnect(false);
                    socket.Dispose();
                }

                pipe.Socket = null;
                _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(pipe));
                // SaveUnsentData(pipe, data);
            }

        }

        private Socket CreateSocketForEndpoint(SendingPipeInfo pipe)
        {
            try
            {
                _logger.Info(string.Format("Creating send socket for endpoint {0}", pipe.Pipe.EndPoint));
                var socket = pipe.Pipe.CreateSocket();
                if (!_transportPipes.Contains(pipe)) //dont add again in case it was detached
                    return null;
                pipe.Socket = socket;

                if (socket == null)
                {
                    _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(pipe));
                    return null;
                }
                return socket;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Error when creating socket for endpoint {0}, mess = {1}", pipe.Pipe.EndPoint, e.Message));
                _timers.Add(_watch.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks, () => CreateSocketForEndpoint(pipe));
                return null;
            }

        }

        //private static void SaveUnsentData(TransportPipe pipe, IList<ArraySegment<byte>> data)
        //{
        //    for (int i = 0; i < data.Count; i++)
        //    {
        //        pipe.MessageContainerConcurrentQueue.PutBackFailedMessage(data[i]);
        //    }
        //}

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
            //_thread.Abort();
            //_thread.Join();

            //foreach (var socket in _endPointToSockets.Values)
            //{
            //    socket.Dispose();
            //}
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