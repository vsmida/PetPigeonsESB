using System;
using System.Collections.Concurrent;
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
        public void perf([Values(150000000 * 2, 150000000, 150000000 / 2)]int size)
        {
            Stopwatch watch = new Stopwatch();
            var queue = new SingleProducerSingleConsumerConcurrentQueue<long>(128);
            var queue2 = new SingleProducerSingleConsumerConcurrentQueue<long>(128);
            bool failed = false;
            Thread t1 = new Thread(() =>
                                       {
                                           try
                                           {
                                               long i = 1;

                                               while (i < size+1)
                                               {
                                                   queue.TryAdd(i);
                                            //       queue2.TryAdd(i);
                                                   i++;
                                               }
                                           }
                                           catch(Exception e)
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
                                           catch(Exception e)
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
            Console.WriteLine(time);
        }


        [Test, Repeat(1)]
        public void perf2([Values(150000000 * 2, 150000000, 150000000 / 2)]int size)
        {
            Stopwatch watch = new Stopwatch();
            var queue = new ConcurrentQueue<long>();
            bool running = true;
            Thread t1 = new Thread(() =>
            {
                long i = 1;
                while (i < size+1)
                {
                    queue.Enqueue(i);
                    i++;
                }
                
            });

            Thread t2 = new Thread(() =>
            {
                long i = 0;
                while (i <  size)
                {
                    queue.TryDequeue(out i);
                }


            });

            Thread t3 = new Thread(() =>
            {
                long i = size +1;
                while (i < 2* size + 1)
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
            watch.Stop();
            var time = watch.ElapsedMilliseconds;
            Console.WriteLine(time);
        }

    }
}