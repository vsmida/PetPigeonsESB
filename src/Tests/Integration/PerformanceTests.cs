using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Bus;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Disruptor.Dsl;
using NUnit.Framework;
using Shared;
using Tests.Transport;
using ZeroMQ;
using log4net;

namespace Tests.Integration
{
    [TestFixture]
    public class PerformanceTests
    {
        private ILog _logger = LogManager.GetLogger(typeof(PerformanceTests));


        [Test]
        public void serializationTests()
        {
            var fakeTransportConfiguration = new FakeTransportConfiguration();
            var endpoint = new ZmqEndpoint(fakeTransportConfiguration.GetConnectEndpoint());
            var wireSendingMessage = new WireSendingMessage(new MessageWireData("test", Guid.NewGuid(), "tt", new byte[0]), endpoint);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var messagesInBatch = 500000;  
            for (int i = 0; i < messagesInBatch; i++)
            {
                var ser = BusSerializer.Serialize(wireSendingMessage);
                BusSerializer.Deserialize<WireSendingMessage>(ser);
            }
            var fps = messagesInBatch / (watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps);
        }

        [Test]
        public void disruptorTests()
        {
            
            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                                        new SingleThreadedClaimStrategy((int)Math.Pow(2,17)), 
                                                                        new SleepingWaitStrategy(), 
                                                                        TaskScheduler.Default);
            disruptor.HandleEventsWith(new eventprocessorTest()).Then(new eventprocessorTest());
            disruptor.Start();
            int size = 150*1000*1000;

            var watch = new Stopwatch();
            watch.Start();
            for(int i = 0; i<size; i++)
            {
                var sequence = disruptor.RingBuffer.Next(TimeSpan.Zero);
                disruptor.RingBuffer.Publish(sequence);
            }

            var wait = new SpinWait();
            while (eventprocessorTest.MessageCount < size)
            {
                wait.SpinOnce();
            }

            watch.Stop();
            var fps = size / (watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps.ToString("N2"));

        }

        class eventprocessorTest : IEventHandler<InboundMessageProcessingEntry>
        {
            public  static int MessageCount;

            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
              //  BusSerializer.Deserialize(data.InitialTransportMessage.Data, TypeUtils.Resolve(typeof(FakePersistingCommand).FullName));
                MessageCount++;
            }
        }

        [Test]
        public void transport_test()
        {
            var transportSend = new ZmqPushWireSendingTransport(ZmqContext.Create());
            transportSend.Initialize();
            var fakeTransportConfiguration = new FakeTransportConfiguration();
            var transportReceive= new ZmqPullWireDataReceiver(ZmqContext.Create(), fakeTransportConfiguration);
            var endpoint = new ZmqEndpoint(fakeTransportConfiguration.GetConnectEndpoint());
            var wireSendingMessage = new WireSendingMessage(new MessageWireData(typeof(FakePersistingCommand).FullName, Guid.NewGuid(), "bus2", BusSerializer.Serialize(new FakePersistingCommand(1))), endpoint);
            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                                         new MultiThreadedClaimStrategy(32768), 
                                                                         new SleepingWaitStrategy(),
                                                                         TaskScheduler.Default);
            disruptor.HandleEventsWith(new eventprocessorTest()).Then(new eventprocessorTest());
            disruptor.Start();
            transportReceive.Initialize(disruptor.RingBuffer);
            transportSend.SendMessage(wireSendingMessage,endpoint);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var messagesCountTotal = 300000;
            for (int i = 0; i < messagesCountTotal; i++)
            {
                transportSend.SendMessage(wireSendingMessage, endpoint);                
            }
            SpinWait wait = new SpinWait();
            while(eventprocessorTest.MessageCount <messagesCountTotal)
            {
                wait.SpinOnce();
            }
            watch.Stop();
            var fps = messagesCountTotal / (watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps);

            transportSend.Dispose();
            transportReceive.Dispose();
        }

        [Test, Repeat(10)]
        public void should_send_messages()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
         //   var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner());
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1);
        //    var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
        //    var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort1, busName1,
      //                                        new SimpleMessageExchange.FakeAssemblyScanner(),
      //                                       new DummyPeerConfig(brokerName, new List<string> { busName2 }));

            bus1.Initialize();
      //      brokerForBus2.Initialize();

      //      bus2.Initialize();
            FakePersistingCommandHandler.OnCommandReceived -= OnPersistingCommandReceived;
            FakePersistingCommandHandler.OnCommandReceived += OnPersistingCommandReceived;

            //small micro-benchmark

            for (int j = 0; j < 5; j++)
            {
                SendMessages(bus1, j);
            }



            bus1.Dispose();
       //     bus2.Dispose();
       //     brokerForBus2.Dispose();

        }

        private void OnPersistingCommandReceived(int obj)
        {
           // _logger.Debug(string.Format("processing command no {0}", obj));
        }

        private static void SendMessages(IBus bus1, int loopNumber)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            IBlockableUntilCompletion resetEvent = null;
            var messagesInBatch = 30000;
            for (int i = 0; i < messagesInBatch; i++)
            {
         //       resetEvent = bus1.Send(new FakePersistingCommand(i * (loopNumber + 1)));
               resetEvent = bus1.Send(new TestData.FakeCommand());
            }

            resetEvent.WaitForCompletion();

            watch.Stop();
            var fps = messagesInBatch/(watch.ElapsedMilliseconds/1000m);
            Console.WriteLine(" FPS : " + fps );
        }
    }
}