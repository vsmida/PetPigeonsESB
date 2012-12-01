using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests.Integration
{
    [ProtoContract]
    [Serializable]
    public class FakeCommand : ICommand
    {
        private static int _lastNumber;
        public static event Action<int> OnLastNumberModified = delegate { };

        [ProtoMember(1, IsRequired = true)]
        public readonly int Number;

        public FakeCommand(int number)
        {
            Number = number;
        }

        public static int LastNumber
        {
            get { return _lastNumber; }
            set
            {
                _lastNumber = value;
                OnLastNumberModified(_lastNumber);
            }
        }
    }

    public class FakeCommandHandler : ICommandHandler<FakeCommand>
    {
        public void Handle(FakeCommand item)
        {
            FakeCommand.LastNumber = item.Number;
        }
    }

    [TestFixture]
    public class SimpleMessageExchange
    {


        [Test, Timeout(10000)]
        public void should_be_able_to_exchange_messages_between_services()
        {
            string location = Directory.GetCurrentDirectory();
            AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;

            var appDomain1 = AppDomain.CreateDomain("Service1Domain", null, setup);
            var appDomain2 = AppDomain.CreateDomain("Service2Domain", null, setup);
            var appDomainDirectoryService = AppDomain.CreateDomain("DirServiceDomain", null, setup);
            foreach (var assembly in Directory.GetFiles(location))
            {
                try
                {
                    appDomain1.Load(AssemblyName.GetAssemblyName(assembly));
                    appDomain2.Load(AssemblyName.GetAssemblyName(assembly));
                    appDomainDirectoryService.Load(AssemblyName.GetAssemblyName(assembly));
                }
                catch (Exception e)
                {
                    //silence in case file is not an assembly
                }
            }
            var testBusCreator1 = appDomain1.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TestBusCreator)).FullName, typeof(TestBusCreator).FullName) as TestBusCreator;
            var testBusCreator2 = appDomain2.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TestBusCreator)).FullName, typeof(TestBusCreator).FullName) as TestBusCreator;
            var testBusCreatorDirService = appDomain2.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TestBusCreator)).FullName, typeof(TestBusCreator).FullName) as TestBusCreator;
            testBusCreatorDirService.CreateFakeDirectoryService();
            var bus1 = testBusCreator1.GetBus("Service1");
            var bus2 = testBusCreator2.GetBus("Service2");

            bus1.Initialize();
            bus2.Initialize();
            var waitHandle = new AutoResetEvent(false);
            FakeCommand.OnLastNumberModified += number =>
                                                    {
                                                        Assert.AreEqual(5, number);
                                                        waitHandle.Set();
                                                    };

            bus1.Send(new FakeCommand(5));
            waitHandle.WaitOne();
        }

        private void OnLastNumberModified(int obj)
        {
            
        }
    }
}