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
        private static event Action<int> _onCommandReceived = delegate { };

        public void Handle(FakeCommand item)
        {
            _onCommandReceived(item.Number);
        }

        public event Action<int> OnCommandReceived
        {
            add { _onCommandReceived += value; }
            remove { _onCommandReceived -= value; }
        }

    }

    [TestFixture]
    public class SimpleMessageExchange : MarshalByRefObject
    {
        private AutoResetEvent _waitForCommandToBeHandled;


        [Test, Timeout(80000)]
        public void should_be_able_to_exchange_messages_between_services()
        {
            AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;

            var appDomain1 = AppDomain.CreateDomain("Service1Domain", null, setup);
            var appDomain2 = AppDomain.CreateDomain("Service2Domain", null, setup);
            var appDomainDirectoryService = AppDomain.CreateDomain("DirServiceDomain", null, setup);

            var testBusCreator1 = appDomain1.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TestBusCreator)).FullName, typeof(TestBusCreator).FullName) as TestBusCreator;
            var testBusCreator2 = appDomain2.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TestBusCreator)).FullName, typeof(TestBusCreator).FullName) as TestBusCreator;
            var testBusCreatorDirService = appDomainDirectoryService.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TestBusCreator)).FullName, typeof(TestBusCreator).FullName) as TestBusCreator;

            testBusCreatorDirService.CreateFakeDirectoryService();
            var bus1 = testBusCreator1.GetBus("Service1");
            var bus2 = testBusCreator2.GetBus("Service2");

            bus2.Initialize();
            bus1.Initialize();

            _waitForCommandToBeHandled = new AutoResetEvent(false);
            var appDomain2FakeCommandHandler  = appDomain2.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof (FakeCommandHandler)).FullName, typeof (FakeCommandHandler).FullName) as FakeCommandHandler;
            appDomain2FakeCommandHandler.OnCommandReceived += OnCommandReceived;
            
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
            Console.WriteLine(" 1000 resend took " +watch.ElapsedMilliseconds+" ms");

            bus1.Dispose();
            bus2.Dispose();
            testBusCreatorDirService.StopDirectoryService();
        }

        private void OnCommandReceived(int number)
        {
            Assert.AreEqual(5, number);
            _waitForCommandToBeHandled.Set();
        }
    }
}