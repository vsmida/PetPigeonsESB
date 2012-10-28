using System;
using Moq;
using NUnit.Framework;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class QosManagerTests
    {
        private QosManager _qosManager;
        private Mock<IQosStrategy> _strategyMock;

        [SetUp]
        public void setup()
        {
            _strategyMock = new Mock<IQosStrategy>();
            _qosManager = new QosManager();
        }

        [Test]
        public void should_let_right_strategy_inspect_message_transportAck()
        {
            var transportAck = new TransportMessage(Guid.NewGuid(), "toto",
                                                    typeof (ReceivedOnTransportAcknowledgement).FullName, new byte[0]);
            _qosManager.RegisterMessage(transportAck, _strategyMock.Object);

            
        }
    }
}