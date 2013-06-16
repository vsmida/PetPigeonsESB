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
            public static List<double> latenciesInMicrosec = new List<double>(260000);
            public static Stopwatch Watch;

            private static readonly PerformanceTests.LatencyMessageSerializer _serializer = new PerformanceTests.LatencyMessageSerializer();


            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
                MessageCount++;
                Watch.Stop();
                var deserialized = _serializer.Deserialize(data.InitialTransportMessage.Data);
                     latenciesInMicrosec.Add((Watch.ElapsedTicks - deserialized.TimeStamp) / (double)(Stopwatch.Frequency) * 1000000);
                     Watch.Start();


            }
        }


        [Test]
        public void custom_transport_test()
        {

            var fakeCOnfig = new DummyCustomTcpTransportConfig {Port = NetworkUtils.GetRandomUnusedPort()};
            var endpoint = new CustomTcpEndpoint(new IPEndPoint(IPAddress.Loopback, fakeCOnfig.Port));
            var transportReceive = new CustomTcpTransportWireDataReceiver(fakeCOnfig,
                                                                          new SerializationHelper(new AssemblyScanner()));

            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                             new MultiThreadedClaimStrategy((int)Math.Pow(2, 15)), 
                                                             new SleepingWaitStrategy(), 
                                                             TaskScheduler.Default);

            disruptor.HandleEventsWith(new EventProcessorInterlockedIncrement());
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
            var transportSend = new CustomTcpWireSendingTransport(new SerializationHelper(new AssemblyScanner()));
            transportSend.Initialize();

            for (int t = 0; t < 50; t++)
            {
                Stopwatch watch = new Stopwatch();
                Console.WriteLine("starting loop");
                var messagesCountTotal = 1000000;
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
                                                                                    typeof (FakePersistingCommand).
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
              
                SpinWait wait = new SpinWait();
                while (EventProcessorInterlockedIncrement.MessageCount < messagesCountTotal)
                {
                    wait.SpinOnce();
                }
                performanceMeasure.Dispose();
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
                                                                         new MultiThreadedClaimStrategy((int)Math.Pow(2, 15)),
                                                                         new SleepingWaitStrategy(),
                                                                         TaskScheduler.Default);
            disruptor.HandleEventsWith(new EventProcessorInterlockedIncrement());
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
           // transportSend.SendMessage(wireSendingMessage, endpoint);
            for (int t = 0;t < 20; t++)
            {
                
                Stopwatch watch = new Stopwatch();
                var messagesCountTotal = 1000000;
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
                                                                                    typeof (FakePersistingCommand).
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
             
                SpinWait wait = new SpinWait();
                while (EventProcessorInterlockedIncrement.MessageCount < messagesCountTotal)
                {
                    wait.SpinOnce();
                }
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