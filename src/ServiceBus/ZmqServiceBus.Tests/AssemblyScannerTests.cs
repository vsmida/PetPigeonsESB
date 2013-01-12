using System.Linq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class AssemblyScannerTests
    {
        private AssemblyScanner _scanner;


        [ProtoContract]
        private class FakeEvent : IEvent
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public FakeEvent(int number)
            {
                Number = number;
            }

        }

        private class FakeEvent2 : IEvent
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public FakeEvent2(int number)
            {
                Number = number;
            }

        }

        private class FakeEventHandler : IBusEventHandler<FakeEvent>, IBusEventHandler<FakeEvent2>
        {
            public static int? NumberInMessage;

            public void Handle(FakeEvent command)
            {
                NumberInMessage = command.Number;
            }

            public void Handle(FakeEvent2 message)
            {
                
            }
        }

        private class FakeCommandHandler : ICommandHandler<FakeCommand>
        {
            public static int? NumberInMessage;

            public void Handle(FakeCommand command)
            {
                NumberInMessage = command.Number;
            }
        }

        [ProtoContract]
        private class FakeCommand : ICommand
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public FakeCommand(int number)
            {
                Number = number;
            }
        }

        [SetUp]
        public void setup()
        {
            _scanner = new AssemblyScanner();
        }

        [Test]
        public void should_find_command_handlers()
        {
            var handleMethods = _scanner.FindCommandHandlersInAssemblies(new FakeCommand(1));
            Assert.AreEqual(1, handleMethods.Count);
            var method = typeof(FakeCommandHandler).GetMethod("Handle");
            Assert.AreEqual(method, handleMethods.Single());
        }

        [Test]
        public void should_find_event_handlers_with_multiple_handle_methods()
        {
            var handleMethods = _scanner.FindEventHandlersInAssemblies(new FakeEvent(1));
            Assert.AreEqual(1, handleMethods.Count);
            var method = typeof(FakeEventHandler).GetMethod("Handle", new[]{typeof(FakeEvent)});
            Assert.AreEqual(method, handleMethods.Single());
        }

        [Test]
        public void should_find_handled_commands()
        {
            var types = _scanner.GetHandledCommands();
            Assert.Contains(typeof(FakeCommand), types);
            Assert.IsFalse(types.Contains(typeof(FakeEvent)));
        }
        
        [Test]
        public void should_find_handled_events()
        {
            var types = _scanner.GetHandledEvents();
            Assert.Contains(typeof(FakeEvent), types);
            Assert.IsFalse(types.Contains(typeof(FakeCommand)));
        }

    }
}