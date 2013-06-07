using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Bus;
using Bus.Dispatch;
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

            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
                Interlocked.Increment(ref MessageCount);
            }
        }

        [Test]
        public void transport_test()
        {
            var transportSend = new ZmqPushWireSendingTransport(ZmqContext.Create(), new AssemblyScanner());
            transportSend.Initialize();
            var fakeTransportConfiguration = new FakeTransportConfiguration();
            var transportReceive = new ZmqPullWireDataReceiver(ZmqContext.Create(), fakeTransportConfiguration, new AssemblyScanner());
            var endpoint = new ZmqEndpoint(fakeTransportConfiguration.GetConnectEndpoint());
            var wireSendingMessage = new WireSendingMessage(new MessageWireData(typeof(FakePersistingCommand).FullName, Guid.NewGuid(), "bus2", BusSerializer.Serialize(new FakePersistingCommand(1))), endpoint);
            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                                         new MultiThreadedClaimStrategy((int)Math.Pow(2, 15)),
                                                                         new SleepingWaitStrategy(),
                                                                         TaskScheduler.Default);
            disruptor.HandleEventsWith(new EventProcessorInterlockedIncrement());
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
            transportSend.SendMessage(wireSendingMessage, endpoint);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var messagesCountTotal = 260000;
            for (int i = 0; i < messagesCountTotal; i++)
            {
                wireSendingMessage = new WireSendingMessage(new MessageWireData(typeof(FakePersistingCommand).FullName, Guid.NewGuid(), "bu7s2", BusSerializer.Serialize(new FakePersistingCommand(1))), endpoint);
                transportSend.SendMessage(wireSendingMessage, endpoint);
            }
            SpinWait wait = new SpinWait();
            while (EventProcessorInterlockedIncrement.MessageCount < messagesCountTotal)
            {
                wait.SpinOnce();
            }
            watch.Stop();
            var fps = messagesCountTotal / (watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps.ToString("N2"));

            transportSend.Dispose();
            transportReceive.Dispose();
        }
    }
}