using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared.Attributes;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;
using IReceivedTransportMessage = ZmqServiceBus.Bus.Transport.IReceivedTransportMessage;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class InternalBusTests
    {

        private InternalBus _bus;
        private Mock<IReceptionLayer> _startupLayerMock;
        private Mock<IMessageDispatcher> _dispatcherMock;
        private Mock<IMessageSender> _messageSenderMock;
        private Mock<IBusBootstrapper> _bootstrapperMock;

        [SetUp]
        public void setup()
        {
            _bootstrapperMock = new Mock<IBusBootstrapper>();
            _startupLayerMock = new Mock<IReceptionLayer>();
            _dispatcherMock = new Mock<IMessageDispatcher>();
            _messageSenderMock = new Mock<IMessageSender>();
            _bus = new InternalBus(_startupLayerMock.Object, _dispatcherMock.Object, _messageSenderMock.Object, _bootstrapperMock.Object);
        }
        [Test]
        public void should_initialize_transport_and_bootstrap_on_start()
        {
            _bus.Initialize();
            _startupLayerMock.Verify(x => x.Initialize());
            _bootstrapperMock.Verify(x => x.BootStrapTopology());
        }

        [Test, Timeout(1000)]
        public void should_dispatch_messages_received()
        {
            var waitForDispatch = new AutoResetEvent(false);
            _bus.Initialize();

            _dispatcherMock.Setup(x => x.Dispatch(It.IsAny<ICommand>())).Callback(() => waitForDispatch.Set());
            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeCommand(5));
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            waitForDispatch.WaitOne();
            _dispatcherMock.Verify(x => x.Dispatch(It.Is<FakeCommand>(y => y.Number == 5)));
        }

        [Test]
        public void should_not_send_ack_when_message_is_already_ack()
        {
            _bus.Initialize();
            var transportMessage = TestData.GenerateDummyReceivedMessage(new CompletionAcknowledgementMessage(Guid.NewGuid(), true));
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage); //processing synchronous for infra messages

            _messageSenderMock.Verify(y => y.Route(It.IsAny<CompletionAcknowledgementMessage>(), transportMessage.PeerName), Times.Never());
        }

        [Test, Timeout(1000)]
        public void should_send_positive_acknowledgement_message_after_successful_dispatch()
        {
            var waitForCompletionMessageSent = new AutoResetEvent(false);
            _messageSenderMock.Setup(x => x.Route(It.IsAny<IMessage>(), It.IsAny<string>())).Callback(
                () => waitForCompletionMessageSent.Set());
            _bus.Initialize();
            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeCommand(5));
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            waitForCompletionMessageSent.WaitOne();
            _messageSenderMock.Verify(y => y.Route(It.Is<CompletionAcknowledgementMessage>(x => x.MessageId == transportMessage.MessageIdentity && x.ProcessingSuccessful == true), transportMessage.PeerName));
        }

        [Test, Timeout(1000)]
        public void should_send_negative_ack_after_unsuccessful_dispatch()
        {
            var waitForCompletionMessageSent = new AutoResetEvent(false);
            _messageSenderMock.Setup(x => x.Route(It.IsAny<IMessage>(), It.IsAny<string>())).Callback(
                () => waitForCompletionMessageSent.Set());
            _bus.Initialize();
            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeCommand(5));
            _dispatcherMock.Setup(x => x.Dispatch(It.IsAny<IMessage>())).Callback<IMessage>(x => { throw new Exception(); });
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            waitForCompletionMessageSent.WaitOne();
            _messageSenderMock.Verify(y => y.Route(It.Is<CompletionAcknowledgementMessage>(x => x.MessageId == transportMessage.MessageIdentity && x.ProcessingSuccessful == false), transportMessage.PeerName));
        }

        [Test, Timeout(1000)]
        public void should_be_able_to_dispatch_infra_messages_while_normal_message_processing_blocks()
        {
            _bus.Initialize();

            var dispatchProcessingWaitHandle = new AutoResetEvent(false);
            bool success = false;
            _dispatcherMock.Setup(x => x.Dispatch(It.IsAny<FakeLongProcessingEvent>())).Callback(() =>
                                                                                                     {
                                                                                                         dispatchProcessingWaitHandle
                                                                                                             .WaitOne();
                                                                                                         success = true;
                                                                                                     });
            var transportMessageStandard = TestData.GenerateDummyReceivedMessage(new FakeLongProcessingEvent());
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessageStandard);
            //blocks
            var transportMessage = TestData.GenerateDummyReceivedMessage(new FakeInfrastructureMessage());
            _dispatcherMock.Setup(x => x.Dispatch(It.IsAny<FakeInfrastructureMessage>())).Callback(() => dispatchProcessingWaitHandle.Set());
            _startupLayerMock.Raise(x => { x.OnMessageReceived += OnMessageReceived; }, transportMessage);

            Assert.IsTrue(success);
        }


        [Test]
        public void should_stop_transport_and_dispatcher_on_stop()
        {
            _bus.Initialize();

            _bus.Dispose();

            _startupLayerMock.Verify(x => x.Dispose());
        }

        private void OnMessageReceived(IReceivedTransportMessage obj)
        {

        }

        [ProtoContract]
        private class FakeLongProcessingEvent : IEvent
        {
        }
        [ProtoContract]
        [InfrastructureMessage]
        private class FakeInfrastructureMessage : ICommand
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