using System;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;
using NUnit.Framework;

namespace ZmqServiceBus.Tests.Disruptor
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

    }
}