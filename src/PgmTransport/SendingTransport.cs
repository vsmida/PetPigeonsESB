using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PgmTransport
{
    public class SendingTransport : IDisposable
    {
        private readonly List<SendingThread> _sendingThreads = new List<SendingThread>();
        private readonly ConcurrentDictionary<TransportPipe, int> _pipesToThreadNumber = new ConcurrentDictionary<TransportPipe, int>();
        public SendingTransport(int numberOfSendingThreads = 1)
        {
            for (int i = 0; i < numberOfSendingThreads; i++)
            {
                _sendingThreads.Add(new SendingThread());
            }
        }

        internal void AttachToIoThread(TransportPipe pipe, int threadNumber = 0)
        {
            if(threadNumber >= _sendingThreads.Count)
                throw new ArgumentException("Sending Thread number exceeds the number of sending threads");
            _sendingThreads[threadNumber].Attach(pipe);
            _pipesToThreadNumber[pipe] = threadNumber;
        }

        public void Dispose()
        {
            foreach (var sendingThread in _sendingThreads)
            {
                sendingThread.Dispose();
            }
        }

        public void DetachFromIoThread(TransportPipe pipe)
        {
            var threadNumber = _pipesToThreadNumber[pipe];
            _sendingThreads[threadNumber].Detach(pipe);
        }
    }
}