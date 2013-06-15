using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Bus;
using Bus.Attributes;
using Bus.MessageInterfaces;
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
using log4net;

namespace Tests.Integration
{
    [TestFixture]
    public class PerformanceTests
    {
        private ILog _logger = LogManager.GetLogger(typeof(PerformanceTests));



        [Test]
        public void disruptorTests()
        {
            
            var disruptor = new Disruptor<InboundMessageProcessingEntry>(() => new InboundMessageProcessingEntry(),
                                                                        new SingleThreadedClaimStrategy((int)Math.Pow(2,17)), 
                                                                        new SleepingWaitStrategy(), 
                                                                        TaskScheduler.Default);
            disruptor.HandleEventsWith(new eventprocessorTest()).Then(new eventprocessorTest2());
            disruptor.Start();
           int size = 150*1000*1000;
           // int size = 2;

            var watch = new Stopwatch();
            watch.Start();
            for(int i = 0; i<size; i++)
            {
                var sequence = disruptor.RingBuffer.Next();
                disruptor.RingBuffer.Publish(sequence);
            }

            var wait = new SpinWait();
            while (eventprocessorTest2.MessageCount < size )
            {
                wait.SpinOnce();
            }

            watch.Stop();
            var fps = size / (watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps.ToString("N2"));
            disruptor.Shutdown();

        }

        class eventprocessorTest : IEventHandler<InboundMessageProcessingEntry>
        {
            public  static int MessageCount;

            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
              //  BusSerializer.Deserialize(data.InitialTransportMessage.Data, TypeUtils.Resolve(typeof(FakePersistingCommand).FullName));
                Interlocked.Increment(ref MessageCount);
            }
        }

        class eventprocessorTest2 : IEventHandler<InboundMessageProcessingEntry>
        {
            public static int MessageCount;


            public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
            {
               // BusSerializer.Deserialize(data.InitialTransportMessage.Data, TypeUtils.Resolve(typeof(FakePersistingCommand).FullName));
                
                Interlocked.Increment(ref MessageCount);
            }
        }

       


        public class LatencyMessage : ICommand
        {
            public readonly long TimeStamp;

            public LatencyMessage(long timeStamp)
            {
                TimeStamp = timeStamp;
            }
        }

        public class LatencyMessageSerializer : BusMessageSerializer<LatencyMessage>
        {
            public override byte[] Serialize(LatencyMessage item)
            {
                return BitConverter.GetBytes(item.TimeStamp);
            }

            public override LatencyMessage Deserialize(byte[] item)
            {
                return new LatencyMessage(BitConverter.ToInt64(item, 0));
            }
        }

        [StaticHandler]
        public class LatencyMessageHandler : ICommandHandler<LatencyMessage>
        {
            public static List<decimal> _latenciesInMicroSeconds  = new List<decimal>();
            public static Stopwatch Watch;
            public void Handle(LatencyMessage item)
            {
                _latenciesInMicroSeconds.Add(((decimal)(Watch.ElapsedTicks - item.TimeStamp))/Stopwatch.Frequency * 1000000);
            }
        }


        [Test, Repeat(10)]
        public void should_send_messages()
        {
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, new PeerId(1), randomPort1, busName1, new PeerId(1), assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner());
      //      var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1);
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, new PeerId(2), randomPort1, busName1, new PeerId(1)); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
       //     var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort1, busName1,
     //                                        new SimpleMessageExchange.FakeAssemblyScanner(),
    //                                         new DummyPeerConfig(brokerName, new List<string> { busName2 }));

            bus1.Initialize();
         //   brokerForBus2.Initialize();

            bus2.Initialize();
            FakePersistingCommandHandler.OnCommandReceived -= OnPersistingCommandReceived;
            FakePersistingCommandHandler.OnCommandReceived += OnPersistingCommandReceived;

            //small micro-benchmark

            for (int j = 0; j < 100; j++)
            {
                var gc0 = GC.CollectionCount(0);
                var gc1 = GC.CollectionCount(1);
                var gc2 = GC.CollectionCount(2);
                SendMessages(bus1, j);
                gc0 = GC.CollectionCount(0) - gc0;
                gc1 = GC.CollectionCount(1) - gc1;
                gc2 = GC.CollectionCount(2) -gc2;

                Console.WriteLine("GC 0 " +gc0);
                Console.WriteLine("GC 1 " +gc1);
                Console.WriteLine("GC 2 " +gc2);
            }



            bus1.Dispose();
           bus2.Dispose();
         //   brokerForBus2.Dispose();

        }

        private void OnPersistingCommandReceived(int obj)
        {
           // _logger.Debug(string.Format("processing command no {0}", obj));
        }

        private static void SendMessages(IBus bus1, int loopNumber)
        {
            Console.WriteLine(" Send Message Loop : ");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            IBlockableUntilCompletion resetEvent = null;
            var messagesInBatch = 40000;
            var fakeCommand = new TestData.FakeCommand();
            LatencyMessageHandler.Watch = watch;
            for (int i = 0; i < messagesInBatch; i++)
            {
                resetEvent = bus1.Send(fakeCommand);
             //   resetEvent = bus1.Send(new LatencyMessage(watch.ElapsedTicks));
                //  bus1.Send(new TestData.FakeCommand()).WaitForCompletion();
            }

            resetEvent.WaitForCompletion();

            watch.Stop();
            var fps = messagesInBatch/(watch.ElapsedMilliseconds/1000m);
            Console.WriteLine(" FPS : " + fps );
            Console.WriteLine(" Elapsed : " + watch.ElapsedTicks / (decimal)Stopwatch.Frequency * 1000000 + " us" );

            LatencyMessageHandler._latenciesInMicroSeconds.Clear();
        }
    }
}