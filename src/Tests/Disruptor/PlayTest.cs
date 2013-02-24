using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;
using NUnit.Framework;

namespace Tests.Disruptor
{
    [TestFixture]
    public class PlayTest
    {
        class InboundMessageEntry
        {
            public int Value;
            public string ValueString;
            public int? NextValue;
        }

        class InboundMessageEntryValueStringProcessor : IEventHandler<InboundMessageEntry>
        {
            public void OnNext(InboundMessageEntry data, long sequence, bool endOfBatch)
            {
                // data.ValueString = data.Value.ToString();
                // Assert.IsNull(data.NextValue);

                Console.WriteLine(data.Value);
            }
        }

        class InboundMessageEntryNextValueProcessor : IEventHandler<InboundMessageEntry>
        {
            public void OnNext(InboundMessageEntry data, long sequence, bool endOfBatch)
            {
                //  data.NextValue = data.Value+1;
                //  Assert.AreEqual(data.Value.ToString(), data.ValueString);
                //Thread.Sleep(100000);
            }
        }

        [Test]
        public void should_run_procesors()
        {
            var disruptor = new Disruptor<InboundMessageEntry>(() => new InboundMessageEntry(),
                                                               new MultiThreadedClaimStrategy(1024),
                                                               new YieldingWaitStrategy(),
                                                               TaskScheduler.Default);

            disruptor.HandleEventsWith(new InboundMessageEntryValueStringProcessor()).Then(
                new InboundMessageEntryNextValueProcessor());

            var ringBuffer = disruptor.Start();

            for (int i = 0; i < 1025; i++)
            {
                long sequenceNo = ringBuffer.Next();
                var entry = ringBuffer[sequenceNo];

                entry.Value = i;
                ringBuffer.Publish(i);

            }

            disruptor.Shutdown();
        }


       
        [Test]
        public void iterating_while_switching()
        {
            var collection1 = Enumerable.Range(0, 10).ToList();
            var collection2 = Enumerable.Range(100, 10).ToList();
            var listOfCollections = new List<List<int>> {collection1, collection2};

        var mySwitchingCollection = collection1;

            Thread t1 = new Thread((collection) =>
                                       {
                                           var list = collection as List<int>;
                                           Stopwatch watch = new Stopwatch();
                                           watch.Start();
                                           while (watch.ElapsedMilliseconds < 1000)
                                           {
                                               foreach (var i in listOfCollections[(int)watch.ElapsedMilliseconds % 2])
                                               {
                                                   Console.WriteLine(i);
                                               }


                                           }

                                       });


            Thread t2 = new Thread((collection) =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                while (watch.ElapsedMilliseconds < 1000)
                {
                    var list1 = listOfCollections[0];
                    listOfCollections[0] = listOfCollections[1];
                    listOfCollections[1] = list1;
                }
            });


            t1.Start(mySwitchingCollection);
            t2.Start(mySwitchingCollection);

            t1.Join();
            t2.Join();

        }

    }
}