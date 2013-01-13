using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using StructureMap;
using Bus;
using Bus.Dispatch;
using Bus.MessageInterfaces;
using Tests.Transport;

namespace Tests
{
    public class MessageDispatcherTests
    {
        private MessageDispatcher _dispatcher;
        private Mock<IContainer> _objectFactoryMock;
        private Mock<IAssemblyScanner> _assemblyScannerMock;

        [SetUp]
        public void setup()
        {
            _objectFactoryMock = new Mock<IContainer>();
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
        public void should_throw_when_multiple_handlers_for_command()
        {
            Assert.Throws<Exception>(() =>_dispatcher.Dispatch(new FakeCommand2(3)));
        }

        [Test, Timeout(1000)]
        public void should_find_event_handlers_and_invoke()
        {
            _dispatcher.Dispatch(new FakeEvent(10));

            Assert.AreEqual(10, FakeEventHandler.NumberInMessage);
            Assert.AreEqual(10, FakeEventHandler_2.NumberInMessage);
        }




        [ProtoContract]
        private class FakeEvent : IEvent
        {
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

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
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }
            
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
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

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
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

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
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

            [ProtoMember(1, IsRequired = true)]
            public int Number;

            public UnknownCommand(int number)
            {
                Number = number;
            }
        }

        private class FakeEventHandler : IBusEventHandler<FakeEvent>
        {
            public static int? NumberInMessage;
            public static AutoResetEvent HandledMessage = new AutoResetEvent(false);

            public void Handle(FakeEvent command)
            {
                NumberInMessage = command.Number;
                HandledMessage.Set();
            }
        }

        private class FakeEventHandler_2 : IBusEventHandler<FakeEvent>
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