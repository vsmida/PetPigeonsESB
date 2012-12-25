using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using Shared;
using StructureMap;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests.Integration
{
    [ProtoContract]
    [Serializable]
    public class FakeCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly int Number;

        public FakeCommand(int number)
        {
            Number = number;
        }

        private FakeCommand()
        {

        }


    }

    public class FakeCommandHandler : MarshalByRefObject, ICommandHandler<FakeCommand>
    {
        public static event Action<int> OnCommandReceived = delegate { };

        public void Handle(FakeCommand item)
        {
            OnCommandReceived(item.Number);
        }

    }

    [TestFixture]
    public class SimpleMessageExchange : MarshalByRefObject
    {
        private AutoResetEvent _waitForCommandToBeHandled;

        [Test, Timeout(5000), Repeat(3)]
        public void should_be_able_to_exchange_messages()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPort3 = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var directoryServiceName = "DirectoryService";
            var bus1 = CreateFakeBus(randomPort1, busName1, randomPort3, directoryServiceName);
            var bus2 = CreateFakeBus(randomPort2, busName2, randomPort3, directoryServiceName);

            IntegrationTestsMockCreator mockCreator = new IntegrationTestsMockCreator();
            mockCreator.CreateFakeDirectoryService(randomPort3);

            bus2.Initialize();
            bus1.Initialize();

            _waitForCommandToBeHandled = new AutoResetEvent(false);
            FakeCommandHandler.OnCommandReceived += OnCommandReceived;

            bus1.Send(new FakeCommand(5));

            _waitForCommandToBeHandled.WaitOne();


            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 1000; i++)
            {
                bus1.Send(new FakeCommand(5));
                _waitForCommandToBeHandled.WaitOne();
            }

            watch.Stop();
            Console.WriteLine(" 1000 resend took " + watch.ElapsedMilliseconds + " ms");
            bus1.Dispose();
            bus2.Dispose();
            
            mockCreator.StopDirectoryService();

        }

        private static IBus CreateFakeBus(int busReceptionPort, string busName, int directoryServicePort, string directoryServiceName)
        {
            return BusFactory.CreateBus(containerConfigurationExpression: ctx =>
                                                                              {
                                                                                  ctx.For
                                                                                      <ZmqTransportConfiguration>()
                                                                                      .Use(
                                                                                          new DummyTransportConfig(
                                                                                              busReceptionPort, busName));
                                                                                  ctx.For
                                                                                      <IBusBootstrapperConfiguration
                                                                                          >().Use(new DummyBootstrapperConfig
                                                                                                      {
                                                                                                          DirectoryServiceEndpoint
                                                                                                              =
                                                                                                              "tcp://localhost:" +
                                                                                                              directoryServicePort,
                                                                                                          DirectoryServiceName
                                                                                                              =
                                                                                                              directoryServiceName

                                                                                                      });
                                                                              });
        }




        private void OnCommandReceived(int number)
        {
            Assert.AreEqual(5, number);
            _waitForCommandToBeHandled.Set();
        }
    }
}