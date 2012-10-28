using System;
using System.Threading;
using DirectoryService.Commands;
using DirectoryService.Event;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Tests.Transport;
using ZmqServiceBus.Transport;
using Serializer = Shared.Serializer;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class BusTests
    {

        private class FakeIBusConfig : IBusConfiguration
        {
            public string DirectoryServiceCommandEndpoint
            {
                get { return "CEndpoint"; }
            }

            public string DirectoryServiceEventEndpoint
            {
                get { return "EEndpoint"; }
            }

            public string ServiceIdentity { get { return "Identity"; }}
        }


        private InternalBus _bus;
        private Mock<ITransport> _transportMock;
        private Mock<IMessageDispatcher> _dispatcherMock;
        private FakeIBusConfig _config;
        private FakeTransportConfiguration _transportConfig;

        [SetUp]
        public void setup()
        {
            _transportMock = new Mock<ITransport>();
            _dispatcherMock = new Mock<IMessageDispatcher>();
            _config = new FakeIBusConfig();
            _transportConfig = new FakeTransportConfiguration();
            _transportMock.SetupGet(x => x.Configuration).Returns(_transportConfig);
            _bus = new InternalBus(_transportMock.Object, _dispatcherMock.Object, _config);
        }
        [Test]
        public void should_initialize_transport_on_start()
        {
            _bus.Initialize();
            _transportMock.Verify(x => x.Initialize(_config.ServiceIdentity));
        }

        [Test]
        public void should_register_directory_service_endpoints_on_start()
        {
            _bus.Initialize();

            _transportMock.Verify(x => x.RegisterCommandHandlerEndpoint<RegisterServiceRelevantMessages>(_config.DirectoryServiceCommandEndpoint));
            _transportMock.Verify(x => x.RegisterPublisherEndpoint<RegisteredHandlersForCommand>(_config.DirectoryServiceEventEndpoint));
            _transportMock.Verify(x => x.RegisterPublisherEndpoint<RegisteredPublishersForEvent>(_config.DirectoryServiceEventEndpoint));
        }

        [Test]
        public void should_register_relevant_types_with_directory_service_on_start()
        {
            ITransportMessage transportMessage = null;
            _transportMock.Setup(x => x.SendMessage(It.IsAny<ITransportMessage>(), It.IsAny<IQosStrategy>())).Callback<ITransportMessage, IQosStrategy>((x, y) => transportMessage = x);
            _bus.Initialize();

            var command = Serializer.Deserialize<RegisterServiceRelevantMessages>(transportMessage.Data);
            Assert.AreEqual(_config.ServiceIdentity, command.ServiceIdentity);
            Assert.AreEqual(_transportConfig.GetCommandsEnpoint(), command.CommandsEndpoint);
            Assert.AreEqual(_transportConfig.GetEventsEndpoint(), command.EventsEndpoint);
            Assert.Contains(typeof(FakeEvent), command.EventsListenedTo);
            Assert.Contains(typeof(FakeCommand), command.CommandsSent);
            Assert.Contains(typeof(FakeCommand), command.HandledCommands);
            Assert.Contains(typeof(FakeEvent), command.EventsListenedTo);
        }


        [Test]
        public void should_dispatch_messages_received()
        {
            _bus.Initialize();

            var transportMessage = new TransportMessage(Guid.NewGuid(), "tt", typeof(FakeCommand).FullName, Serializer.Serialize(new FakeCommand(5)));
            _transportMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            _dispatcherMock.Verify(x => x.Dispatch(It.Is<FakeCommand>(y => y.Number == 5)));
        }

        [Test]
        public void should_send_positive_acknowledgement_message_after_successful_dispatch()
        {
            _bus.Initialize();
            var transportMessage = new TransportMessage(Guid.NewGuid(), "tt", typeof(FakeCommand).FullName, Serializer.Serialize(new FakeCommand(5)));
            _transportMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            _transportMock.Verify(x => x.AckMessage(transportMessage.SenderIdentity, transportMessage.MessageIdentity, true));
        }

        [Test]
        public void should_send_negative_ack_after_unsuccessful_dispatch()
        {
            _bus.Initialize();
            var transportMessage = new TransportMessage(Guid.NewGuid(), "tt", typeof(FakeCommand).FullName, Serializer.Serialize(new FakeCommand(5)));
            _dispatcherMock.Setup(x => x.Dispatch(It.IsAny<IMessage>())).Callback<IMessage>(x => { throw new Exception(); });
            _transportMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            _transportMock.Verify(x => x.AckMessage(transportMessage.SenderIdentity, transportMessage.MessageIdentity, false));

        }

  

        [Test]
        public void should_stop_transport_on_stop()
        {
            _bus.Initialize();

            _bus.Dispose();

            _transportMock.Verify(x => x.Dispose());
        }

        private void OnMessageReceived(ITransportMessage obj)
        {

        }

        [ProtoContract]
        [InfrastructureMessage]
        private class FakeCommand : ICommand
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public FakeCommand(int number)
            {
                Number = number;
            }
        }

        private class FakeCommandHandler : ICommandHandler<FakeCommand>
        {
            public void Handle(FakeCommand command)
            {

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


        private class FakeEventHandler : IEventHandler<FakeEvent>
        {
            public void Handle(FakeEvent message)
            {

            }
        }


    }
}