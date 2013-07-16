using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
using Shared;
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
            public static List<double> latenciesInMicrosec = new List<double>(2000000);
            public static Stopwatch Watch;

            private static readonly PerformanceTests.LatencyMessageSerializer _serializer = new PerformanceTests.LatencyMessageSerializer();
            private AutoResetEvent _waitHandleForNumberOfMessages;
            private int _messagesCountTotal;

            public EventProcessorInterlockedIncrement(AutoResetEvent waitHandleForNumberOfMessages, int messagesCountTotal)
            {
                _messagesCountTotal = messagesCountTotal;
                _waitHandleForNumberOfMessages = waitHandleForNumberOfMessages;
            }


            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
                MessageCount++;
                Watch.Stop();
              //  var x = 3;
         //       while (x < 1000)
       //             x++;
               // Thread.SpinWait(100);
                var deserialized = _serializer.Deserialize(data.InitialTransportMessage.Data);
                latenciesInMicrosec.Add((Watch.ElapsedTicks - deserialized.TimeStamp) / (double)(Stopwatch.Frequency) * 1000000);
                Watch.Start();
                if (MessageCount == _messagesCountTotal)
                    _waitHandleForNumberOfMessages.Set();


            }
        }


        [Test]
        public void custom_transport_test()
        {
           // Thread.BeginThreadAffinity();
           //ThreadUtils.CurrentProcessThread.ProcessorAffinity = new IntPtr(1 << 1);
            var fakeCOnfig = new DummyCustomTcpTransportConfig { Port = NetworkUtils.GetRandomUnusedPort() };
            var endpoint = new CustomTcpEndpoint(new IPEndPoint(IPAddress.Loopback, fakeCOnfig.Port));
        //    var taskScheduler = new RoundRobinThreadAffinedTaskScheduler(1, 1);
       //     var taskScheduler2 = new RoundRobinThreadAffinedTaskScheduler(1, 0);
            //     var taskScheduler = TaskScheduler.Current;
            var transportReceive = new CustomTcpTransportWireDataReceiver(fakeCOnfig,
                                                                          new SerializationHelper(new AssemblyScanner()));

            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                             new MultiThreadedClaimStrategy((int)Math.Pow(2, 13)),
                                                             new SleepingWaitStrategy(),
                                                             TaskScheduler.Current);
            AutoResetEvent waitHandleForNumberOfMessages = new AutoResetEvent(false);
            var messagesCountTotal = 1000 * 1000 * 1;
            disruptor.HandleEventsWith(new EventProcessorInterlockedIncrement(waitHandleForNumberOfMessages, messagesCountTotal));
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
            var transportSend = new CustomTcpWireSendingTransport(new SerializationHelper(new AssemblyScanner()), null);
            transportSend.Initialize();

            for (int t = 0; t < 10; t++)
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
                Stopwatch watch = new Stopwatch();
                Console.WriteLine("starting loop");
                // var messagesCountTotal = 10;
                var serializer = new PerformanceTests.LatencyMessageSerializer();
                EventProcessorInterlockedIncrement.Watch = watch;
                var performanceMeasure = new PerformanceMeasure(() =>
                                                                    {
                                                                        watch.Stop();
                                                                        var data =
                                                                            serializer.Serialize(
                                                                                new PerformanceTests.LatencyMessage(
                                                                                    watch.ElapsedTicks));

                                                                        var wireSendingMessage =
                                                                            new WireSendingMessage(
                                                                                new MessageWireData(
                                                                                    typeof(FakePersistingCommand).
                                                                                        FullName,
                                                                                    Guid.Empty,
                                                                                    new PeerId(11),
                                                                                    data),
                                                                                endpoint);

                                                                        watch.Start();

                                                                        transportSend.SendMessage(wireSendingMessage,
                                                                                                  endpoint);

                                                                    },
                                                                messagesCountTotal);

                waitHandleForNumberOfMessages.WaitOne();


                performanceMeasure.Dispose();
                Thread.Sleep(500);
                if (EventProcessorInterlockedIncrement.MessageCount > messagesCountTotal)
                    Assert.Fail("Too many messages");
                EventProcessorInterlockedIncrement.MessageCount = 0;

                var statistics = EventProcessorInterlockedIncrement.latenciesInMicrosec.ComputeStatistics();
                Console.WriteLine(statistics);
                EventProcessorInterlockedIncrement.latenciesInMicrosec.Clear();
            }
            transportSend.Dispose();
            transportReceive.Dispose();
        }

        [Test]
        public void zmq_transport_test()
        {
            var transportSend = new ZmqPushWireSendingTransport(ZmqContext.Create(), new SerializationHelper(new AssemblyScanner()));
            transportSend.Initialize();
            var fakeTransportConfiguration = new FakeTransportConfiguration();
            var transportReceive = new ZmqPullWireDataReceiver(ZmqContext.Create(), fakeTransportConfiguration, new SerializationHelper(new AssemblyScanner()));
            var endpoint = new ZmqEndpoint(fakeTransportConfiguration.GetConnectEndpoint());
            var wireSendingMessage = new WireSendingMessage(new MessageWireData(typeof(FakePersistingCommand).FullName, Guid.NewGuid(), new PeerId(4), BusSerializer.Serialize(new FakePersistingCommand(1))), endpoint);
            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                                         new MultiThreadedClaimStrategy((int)Math.Pow(2, 13)),
                                                                         new SleepingWaitStrategy(),
                                                                         TaskScheduler.Current);
            AutoResetEvent waitHandleForNumberOfMessages = new AutoResetEvent(false);
            var messagesCountTotal = 1000 * 1000 * 1;
            disruptor.HandleEventsWith(new EventProcessorInterlockedIncrement(waitHandleForNumberOfMessages, messagesCountTotal));
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
            // transportSend.SendMessage(wireSendingMessage, endpoint);
            for (int t = 0; t < 20; t++)
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
                Stopwatch watch = new Stopwatch();
                // var messagesCountTotal = 10;
                var serializer = new PerformanceTests.LatencyMessageSerializer();
                EventProcessorInterlockedIncrement.Watch = watch;
                var performanceMeasure = new PerformanceMeasure(() =>
                                                                    {
                                                                        watch.Stop();
                                                                        var data =
                                                                            serializer.Serialize(
                                                                                new PerformanceTests.LatencyMessage(
                                                                                    watch.ElapsedTicks));

                                                                        wireSendingMessage =
                                                                            new WireSendingMessage(
                                                                                new MessageWireData(
                                                                                    typeof(FakePersistingCommand).
                                                                                        FullName,
                                                                                    Guid.NewGuid(),
                                                                                    new PeerId(44),
                                                                                    data),
                                                                                endpoint);
                                                                        watch.Start();
                                                                        transportSend.SendMessage(wireSendingMessage,
                                                                                                  endpoint);
                                                                    },
                                                                messagesCountTotal,
                                                                watch);

                waitHandleForNumberOfMessages.WaitOne();

                performanceMeasure.Dispose();
                EventProcessorInterlockedIncrement.MessageCount = 0;
                var statistics = EventProcessorInterlockedIncrement.latenciesInMicrosec.ComputeStatistics();
                Console.WriteLine(statistics);
                EventProcessorInterlockedIncrement.latenciesInMicrosec.Clear();
            }
            transportSend.Dispose();
            transportReceive.Dispose();
        }
    }
}