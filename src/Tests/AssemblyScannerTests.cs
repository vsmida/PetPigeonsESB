using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bus;
using Bus.Attributes;
using Bus.Dispatch;
using Bus.MessageInterfaces;
using Bus.Subscriptions;
using Bus.Transport.Network;
using NUnit.Framework;
using ProtoBuf;
using Shared;

namespace Tests
{
    [TestFixture]
    public class AssemblyScannerTests
    {
        private AssemblyScanner _scanner;

        [SubscriptionFilterAttributeActive(true)]
        private class FakeSubscriptionFilter : ISubscriptionFilter<FakeCommand>
        {
            public bool Matches(IMessage item)
            {
                return false;
            }
        }

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
        [BusOptions(ReliabilityLevel.Persisted, WireTransportType.ZmqPushPullTransport)]
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
        public void should_find_messages_options()
        {
            var options = _scanner.GetMessageOptions();
            var fakeCommandOptions = options.Single(x => x.MessageType == typeof (FakeCommand));
            var fakeEventOptions = options.Single(x => x.MessageType == typeof (FakeEvent));

            Assert.AreEqual(ReliabilityLevel.Persisted, fakeCommandOptions.ReliabilityLevel);
            Assert.AreEqual(WireTransportType.ZmqPushPullTransport, fakeCommandOptions.TransportType);
            Assert.AreEqual(typeof(FakeSubscriptionFilter), fakeCommandOptions.SubscriptionFilter.GetType());

            Assert.AreEqual(ReliabilityLevel.FireAndForget, fakeEventOptions.ReliabilityLevel);
            Assert.AreEqual(WireTransportType.ZmqPushPullTransport, fakeEventOptions.TransportType);
            Assert.AreEqual(null, fakeEventOptions.SubscriptionFilter);

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