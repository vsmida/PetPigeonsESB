using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Bus;
using Bus.Dispatch;
using Bus.Serializer;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Disruptor.Dsl;
using NUnit.Framework;
using Tests.Transport;
using ZeroMQ;

namespace Tests.Integration.Performance
{
    [TestFixture]
    public class Transports
    {
        private class EventProcessorInterlockedIncrement : IEventHandler<InboundMessageProcessingEntry>
        {
            public static int MessageCount;
            public static List<decimal> latenciesInMicrosec = new List<decimal>(260000);
            public static Stopwatch Watch;

            private static readonly PerformanceTests.LatencyMessageSerializer _serializer = new PerformanceTests.LatencyMessageSerializer();


            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
                MessageCount++;
                Watch.Stop();
                var deserialized = _serializer.Deserialize(data.InitialTransportMessage.Data);
                     latenciesInMicrosec.Add((Watch.ElapsedTicks - deserialized.TimeStamp) / (decimal)(Stopwatch.Frequency) * 1000000);
                     Watch.Start();


            }
        }

        [Test]
        public void transport_test()
        {
            var transportSend = new ZmqPushWireSendingTransport(ZmqContext.Create(), new SerializationHelper(new AssemblyScanner()));
            transportSend.Initialize();
            var fakeTransportConfiguration = new FakeTransportConfiguration();
            var transportReceive = new ZmqPullWireDataReceiver(ZmqContext.Create(), fakeTransportConfiguration, new SerializationHelper(new AssemblyScanner()));
            var endpoint = new ZmqEndpoint(fakeTransportConfiguration.GetConnectEndpoint());
            var wireSendingMessage = new WireSendingMessage(new MessageWireData(typeof(FakePersistingCommand).FullName, Guid.NewGuid(), new PeerId(4), BusSerializer.Serialize(new FakePersistingCommand(1))), endpoint);
            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                                         new MultiThreadedClaimStrategy((int)Math.Pow(2, 15)),
                                                                         new SleepingWaitStrategy(),
                                                                         TaskScheduler.Default);
            disruptor.HandleEventsWith(new EventProcessorInterlockedIncrement());
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
           // transportSend.SendMessage(wireSendingMessage, endpoint);
            for (int t = 0;t < 100; t++)
            {
                
                Stopwatch watch = new Stopwatch();
                watch.Start();
                var messagesCountTotal = 100000;
               // var messagesCountTotal = 10;
                var serializer = new PerformanceTests.LatencyMessageSerializer();
                EventProcessorInterlockedIncrement.Watch = watch;
                for (int i = 0; i < messagesCountTotal; i++)
                {
                    watch.Stop();
                    var data = serializer.Serialize(new PerformanceTests.LatencyMessage(watch.ElapsedTicks));

                    wireSendingMessage =
                        new WireSendingMessage(
                            new MessageWireData(typeof (FakePersistingCommand).FullName, Guid.NewGuid(), new PeerId(44), data),
                            endpoint);
                    watch.Start();
                    transportSend.SendMessage(wireSendingMessage, endpoint);
                }
                SpinWait wait = new SpinWait();
                while (EventProcessorInterlockedIncrement.MessageCount < messagesCountTotal)
                {
                    wait.SpinOnce();
                }
                watch.Stop();
                var fps = messagesCountTotal/(watch.ElapsedTicks/ (double)Stopwatch.Frequency);
                Console.WriteLine(" FPS : " + fps.ToString("N2"));
                EventProcessorInterlockedIncrement.latenciesInMicrosec.Clear();
            }
            transportSend.Dispose();
            transportReceive.Dispose();
        }
    }
}