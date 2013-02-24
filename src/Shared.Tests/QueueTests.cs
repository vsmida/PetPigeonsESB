using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Shared;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class QueueTests
    {
        [Test, Repeat(1)]
        public void perf([Values(150 * 1000 * 1000 * 2, 150 * 1000 * 1000, 150 * 1000 * 1000 / 2)]int size)
        {
            Stopwatch watch = new Stopwatch();
            var queue = new SingleProducerSingleConsumerConcurrentQueue<long>(256);
            var queue2 = new SingleProducerSingleConsumerConcurrentQueue<long>(256);
            bool failed = false;
            Thread t1 = new Thread(() =>
                                       {
                                           try
                                           {
                                               long i = 1;

                                               while (i < size + 1)
                                               {
                                                   queue.TryAdd(i);
                                                   //       queue2.TryAdd(i);
                                                   i++;
                                               }
                                           }
                                           catch (Exception e)
                                           {
                                               failed = true;
                                           }

                                       });

            Thread t2 = new Thread(() =>
                                       {
                                           try
                                           {
                                               long i = 0;
                                               //long j = 0;
                                               while (i < size) //&& j < size)
                                               {
                                                   queue.TryTake(out i);
                                                   //        queue2.TryTake(out j);


                                               }
                                           }
                                           catch (Exception e)
                                           {

                                               failed = true;
                                           }
                                       });


            Thread t3 = new Thread(() =>
            {
                try
                {
                    long i = 0;
                    SpinWait wait = new SpinWait();

                    while (i < size + 1)
                    {
                        queue2.TryAdd(i);
                        i++;
                        wait.SpinOnce();
                    }
                }
                catch (Exception e)
                {

                    failed = true;
                }
            });


            watch.Start();
            t1.Start();
            t2.Start();
            //   t3.Start();


            t1.Join();
            t2.Join();
            //     t3.Join();
            Assert.IsFalse(failed);
            watch.Stop();
            var time = watch.ElapsedMilliseconds;
            var fps = size / time * 1000;
            Console.WriteLine(string.Format("FPS : {0}", fps.ToString("N2")));
        }


        [Test, Repeat(1)]
        public void perf2([Values(150 * 1000 * 1000 * 2, 150 * 1000 * 1000, 150 * 1000 * 1000 / 2)]int size)
        {
            Stopwatch watch = new Stopwatch();
            var queue = new ConcurrentQueue<long>();
            bool running = true;
            Thread t1 = new Thread(() =>
            {
                long i = 1;
                while (i < size + 1)
                {
                    queue.Enqueue(i);
                    i++;
                }

            });

            Thread t2 = new Thread(() =>
            {
                long i = 0;
                while (i < size)
                {
                    queue.TryDequeue(out i);
                }


            });

            Thread t3 = new Thread(() =>
            {
                long i = size + 1;
                while (i < 2 * size + 1)
                {
                    queue.Enqueue(i);
                    i++;
                }

            });

            watch.Start();
            t1.Start();
            t2.Start();
            //   t3.Start();

            t1.Join();
            t2.Join();
            //     t3.Join();
            var time = watch.ElapsedMilliseconds;
            var fps = size / time * 1000;
            Console.WriteLine(string.Format("FPS : {0}", fps.ToString("N2")));
        }


        [Test, Repeat(1)]
        public void perf3([Values(10 * 1000 * 1000 * 2, 10 * 1000 * 1000, 10 * 1000 * 1000 / 2)]int size)
        {
            Stopwatch watch = new Stopwatch();
            var queue = new Queue<long>();
            bool running = true;
            object sync = new object();
            Thread t1 = new Thread(() =>
            {
                long i = 1;
                while (i < size + 1)
                {
                    lock (sync)
                        queue.Enqueue(i);
                    i++;
                }

            });

            Thread t2 = new Thread(() =>
            {
                long i = 0;
                var wait = new SpinWait();
                while (i < size)
                {
                    lock (sync)
                    {
                        if (queue.Count > 0)
                            queue.Dequeue();
                        else
                        {
                            wait.SpinOnce();
                        }
                    }

                }


            });

            Thread t3 = new Thread(() =>
            {
                long i = size + 1;
                while (i < 2 * size + 1)
                {
                    queue.Enqueue(i);
                    i++;
                }

            });

            watch.Start();
            t1.Start();
            t2.Start();
            //   t3.Start();

            t1.Join();
            t2.Join();
            //     t3.Join();
            var time = watch.ElapsedMilliseconds;
            var fps = size / time * 1000;
            Console.WriteLine(string.Format("FPS : {0}", fps.ToString("N2")));
        }

    }
}