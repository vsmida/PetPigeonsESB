using System;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared.Attributes;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;
using IReceivedTransportMessage = ZmqServiceBus.Bus.Transport.IReceivedTransportMessage;

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

            public string ServiceIdentity { get { return "Identity"; } }
        }


        private InternalBus _bus;
        private Mock<IReceptionLayer> _startupLayerMock;
        private Mock<IMessageDispatcher> _dispatcherMock;
        private FakeIBusConfig _config;
        private Mock<IMessageSender> _messageSenderMock;

        [SetUp]
        public void setup()
        {
            _startupLayerMock = new Mock<IReceptionLayer>();
            _dispatcherMock = new Mock<IMessageDispatcher>();
            _config = new FakeIBusConfig();
            _messageSenderMock = new Mock<IMessageSender>();
            _bus = new InternalBus(_startupLayerMock.Object, _dispatcherMock.Object, _messageSenderMock.Object);
        }
        [Test]
        public void should_initialize_transport_on_start()
        {
            _bus.Initialize();
            _startupLayerMock.Verify(x => x.Initialize());
        }



        [Test]
        public void should_dispatch_messages_received()
        {
            _bus.Initialize();

            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeCommand(5));
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            _dispatcherMock.Verify(x => x.Dispatch(It.Is<FakeCommand>(y => y.Number == 5)));
        }

        [Test]
        public void should_send_positive_acknowledgement_message_after_successful_dispatch()
        {
            _bus.Initialize();
            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeCommand(5));
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            _messageSenderMock.Verify(y => y.Route(It.Is<AcknowledgementMessage>(x => x.MessageId == transportMessage.MessageIdentity && x.ProcessingSuccessful == true), transportMessage.PeerName));
        }

        [Test]
        public void should_send_negative_ack_after_unsuccessful_dispatch()
        {
            _bus.Initialize();
            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeCommand(5));
            _dispatcherMock.Setup(x => x.Dispatch(It.IsAny<IMessage>())).Callback<IMessage>(x => { throw new Exception(); });
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            _messageSenderMock.Verify(y => y.Route(It.Is<AcknowledgementMessage>(x => x.MessageId == transportMessage.MessageIdentity && x.ProcessingSuccessful == false), transportMessage.PeerName));
        }



        [Test]
        public void should_stop_transport_on_stop()
        {
            _bus.Initialize();

            _bus.Dispose();

            _startupLayerMock.Verify(x => x.Dispose());
        }

        private void OnMessageReceived(IReceivedTransportMessage obj)
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