using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;

namespace ZmqServiceBus.Tests
{
    public class MessageDispatcherTests
    {
        private MessageDispatcher _dispatcher;
        private Mock<IObjectFactory> _objectFactoryMock;
        private Mock<IAssemblyScanner> _assemblyScannerMock;

        [SetUp]
        public void setup()
        {
            _objectFactoryMock = new Mock<IObjectFactory>();
            _assemblyScannerMock = new Mock<IAssemblyScanner>();
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(FakeCommandHandler))).Returns(new FakeCommandHandler());
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(FakeEventHandler))).Returns(new FakeEventHandler());
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(FakeEventHandler_2))).Returns(new FakeEventHandler_2());
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(TestData.CommandThatThrowsHandler))).Returns(new TestData.CommandThatThrowsHandler());
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(TestData.FakeCommandHandler))).Returns(new TestData.FakeCommandHandler());
            _assemblyScannerMock.Setup(x => x.FindCommandHandlersInAssemblies(It.IsAny<FakeCommand>())).Returns(
                new List<MethodInfo> { typeof(FakeCommandHandler).GetMethod("Handle") });
            _assemblyScannerMock.Setup(x => x.FindCommandHandlersInAssemblies(It.IsAny<UnknownCommand>())).Returns(new List<MethodInfo>());
            _assemblyScannerMock.Setup(x => x.FindCommandHandlersInAssemblies(It.IsAny<TestData.FakeCommand>())).Returns(new List<MethodInfo>{ typeof(TestData.FakeCommandHandler).GetMethod("Handle")});
            _assemblyScannerMock.Setup(x => x.FindCommandHandlersInAssemblies(It.IsAny<TestData.CommandThatThrows>())).Returns(new List<MethodInfo>{ typeof(TestData.CommandThatThrowsHandler).GetMethod("Handle")});
            _assemblyScannerMock.Setup(x => x.FindEventHandlersInAssemblies(It.IsAny<UnknownEvent>())).Returns(new List<MethodInfo>());
            _assemblyScannerMock.Setup(x => x.FindCommandHandlersInAssemblies(It.IsAny<FakeCommand2>())).Returns(new List<MethodInfo> { typeof(FakeCommandHandler2_1).GetMethod("Handle"), typeof(FakeCommandHandler2_2).GetMethod("Handle") });
            _assemblyScannerMock.Setup(x => x.FindEventHandlersInAssemblies(It.IsAny<FakeEvent>())).Returns(new List<MethodInfo> { typeof(FakeEventHandler).GetMethod("Handle"), typeof(FakeEventHandler_2).GetMethod("Handle") });
            _dispatcher = new MessageDispatcher(_objectFactoryMock.Object, _assemblyScannerMock.Object);
            FakeCommandHandler.NumberInMessage = null;
            FakeCommandHandler.NumberSet.WaitOne();
        }

        [Test, Timeout(1000)]
        public void should_find_command_handler_and_invoke()
        {
            _dispatcher.Dispatch(new FakeCommand(3));

            FakeCommandHandler.NumberSet.WaitOne();
            Assert.AreEqual(3, FakeCommandHandler.NumberInMessage);
        }

        [Test]
        public void should_do_nothing_when_receiving_command_with_no_handler()
        {
            Assert.DoesNotThrow(() => _dispatcher.Dispatch(new UnknownCommand(3)));
        }

        [Test]
        public void should_do_nothing_when_receiving_event_with_no_handler()
        {
            Assert.DoesNotThrow(() => _dispatcher.Dispatch(new UnknownEvent(3)));
        }

        [Test, Timeout(1000)]
        public void should_raise_error_occured_when_error()
        {
            var waitHandle = new AutoResetEvent(false);
            var command = new TestData.CommandThatThrows();
            _dispatcher.Dispatch(command);
            _dispatcher.ErrorOccurred += (mess, ex) =>
                                             {
                                                 Assert.AreEqual(command, mess);
                                                 Assert.AreEqual("throwing", ex.InnerException.Message);
                                                 waitHandle.Set();
                                             };
            waitHandle.WaitOne();

        }

        [Test, Timeout(1000)]
        public void should_raise_dispatch_successful_when_ok()
        {
            var waitHandle = new AutoResetEvent(false);
            var command = new TestData.FakeCommand();
            _dispatcher.Dispatch(command);
            _dispatcher.SuccessfulDispatch += (mess) =>
            {
                Assert.AreEqual(command, mess);
                waitHandle.Set();
            };
            waitHandle.WaitOne();

        }

        [Test, Timeout(1000)]
        public void should_throw_when_multiple_handlers_for_command()
        {
            var waitHandle = new AutoResetEvent(false);
            _dispatcher.ErrorOccurred += (mess, ex) => waitHandle.Set();
            _dispatcher.Dispatch(new FakeCommand2(3));
            waitHandle.WaitOne();
        }

        [Test, Timeout(1000)]
        public void should_find_event_handlers_and_invoke()
        {
            _dispatcher.Dispatch(new FakeEvent(10));

            FakeEventHandler.HandledMessage.WaitOne();
            Assert.AreEqual(10, FakeEventHandler.NumberInMessage);
            FakeEventHandler_2.HandledMessage.WaitOne();
            Assert.AreEqual(10, FakeEventHandler_2.NumberInMessage);
        }


        [Test, Timeout(1000)]
        public void should_be_able_to_dispatch_infra_messages_while_doing_other_stuff()
        {
            _assemblyScannerMock.Setup(x => x.FindCommandHandlersInAssemblies(It.IsAny<FakeInfrastructureMessage>()))
                                .Returns(new List<MethodInfo> { typeof(FakeInfrastructureMessageHandler).GetMethod("Handle") });
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(FakeInfrastructureMessageHandler))).Returns(new FakeInfrastructureMessageHandler());

            _assemblyScannerMock.Setup(x => x.FindEventHandlersInAssemblies(It.IsAny<FakeLongProcessingEvent>()))
                                .Returns(new List<MethodInfo> { typeof(FakeLongProcessingEventHandler).GetMethod("Handle") });
            _objectFactoryMock.Setup(x => x.GetInstance(typeof(FakeLongProcessingEventHandler))).Returns(new FakeLongProcessingEventHandler());

            _dispatcher.Dispatch(new FakeLongProcessingEvent(7));
            _dispatcher.Dispatch(new FakeInfrastructureMessage());

            FakeLongProcessingEventHandler.WaitForHandleCompleted.WaitOne();
            Assert.AreEqual(7, FakeLongProcessingEvent.LastProcessedNumber);

        }


        private class FakeLongProcessingEvent : IEvent
        {
            public static int LastProcessedNumber;

            public int Number;

            public FakeLongProcessingEvent(int number)
            {
                Number = number;
            }
        }

        private class FakeLongProcessingEventHandler : IEventHandler<FakeLongProcessingEvent>
        {
            public static readonly AutoResetEvent WaitForHandleCompleted = new AutoResetEvent(false);

            public void Handle(FakeLongProcessingEvent message)
            {
                FakeInfrastructureMessageHandler.WaitHandle.WaitOne();
                FakeLongProcessingEvent.LastProcessedNumber = message.Number;
                WaitForHandleCompleted.Set();
            }
        }

        [InfrastructureMessage]
        private class FakeInfrastructureMessage : ICommand
        {

        }

        private class FakeInfrastructureMessageHandler : ICommandHandler<FakeInfrastructureMessage>
        {
            public static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
            public void Handle(FakeInfrastructureMessage item)
            {

                WaitHandle.Set();
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


        [ProtoContract]
        private class UnknownEvent : IEvent
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public UnknownEvent(int number)
            {
                Number = number;
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

        [ProtoContract]
        private class FakeCommand2 : ICommand
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public FakeCommand2(int number)
            {
                Number = number;
            }
        }

        [ProtoContract]
        private class UnknownCommand : ICommand
        {
            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public UnknownCommand(int number)
            {
                Number = number;
            }
        }

        private class FakeEventHandler : IEventHandler<FakeEvent>
        {
            public static int? NumberInMessage;
            public static AutoResetEvent HandledMessage = new AutoResetEvent(false);

            public void Handle(FakeEvent command)
            {
                NumberInMessage = command.Number;
                HandledMessage.Set();
            }
        }

        private class FakeEventHandler_2 : IEventHandler<FakeEvent>
        {
            public static int? NumberInMessage;
            public static AutoResetEvent HandledMessage = new AutoResetEvent(false);


            public void Handle(FakeEvent command)
            {
                NumberInMessage = command.Number;
                HandledMessage.Set();
            }
        }

        private class FakeCommandHandler : ICommandHandler<FakeCommand>
        {
            private static int? _numberInMessage;

            public static readonly AutoResetEvent NumberSet = new AutoResetEvent(false);
            public static int? NumberInMessage
            {
                get { return _numberInMessage; }
                set
                {
                    _numberInMessage = value;
                    NumberSet.Set();
                }
            }

            public void Handle(FakeCommand command)
            {
                NumberInMessage = command.Number;
            }
        }

        private class FakeCommandHandler2_1 : ICommandHandler<FakeCommand2>
        {
            public static int? NumberInMessage;

            public void Handle(FakeCommand2 command)
            {
                NumberInMessage = command.Number;
            }
        }

        private class FakeCommandHandler2_2 : ICommandHandler<FakeCommand2>
        {
            public static int? NumberInMessage;

            public void Handle(FakeCommand2 command)
            {
                NumberInMessage = command.Number;
            }
        }


    }
}