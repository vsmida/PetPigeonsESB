using System;
using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;

namespace PgmTransportTests
{
    [TestFixture]
    public class QueueTests
    {
        private ConcurrentQueue<int> _insertionQueue = new ConcurrentQueue<int>();
        private ConcurrentQueue<int> _switchQueue = new ConcurrentQueue<int>();
        private SpinLock spinLock = new SpinLock();

        private int _currentNumber = 0;

        [Test, Repeat(10)]
        public void can_steal()
        {
            var batch = 100000000;
            Console.WriteLine("Starting");
            bool failed = false;
            var thread = new Thread(() =>
                                        {
                                            while (true)
                                            {
                                                ConcurrentQueue<int> queue = null;
                                                bool locktaken = false;
                                                spinLock.Enter(ref locktaken);
                                                queue = _insertionQueue;
                                                _insertionQueue = _switchQueue;
                                                spinLock.Exit();
                                                while (queue.Count > 0)
                                                {
                                                    int data;
                                                    queue.TryDequeue(out data);

                                                    if (data != _currentNumber)
                                                    {
                                                        failed = true;
                                                        return;
                                                    }
                                                    _currentNumber++;

                                                    if (_currentNumber == batch)
                                                        return;
                                                }
                                                Interlocked.Exchange(ref _switchQueue, queue);
                                            }

                                        });

            var insertionThread = new Thread(() =>
                                                 {
                                                     for (int i = 0; i < batch; i++)
                                                     {
                                                         bool locktaken = false;
                                                         spinLock.Enter(ref locktaken);
                                                         _insertionQueue.Enqueue(i);
                                                         spinLock.Exit();

                                                     }
                                                 });

            thread.Start();
            insertionThread.Start();

            thread.Join();
            insertionThread.Join();
            _currentNumber = 0;
            Assert.IsFalse(failed, "The numbers were not in sequence");

        }

    }
}