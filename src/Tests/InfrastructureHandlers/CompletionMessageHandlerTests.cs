﻿using System;
using Moq;
using NUnit.Framework;
using Bus;
using Bus.Handlers;
using Bus.InfrastructureMessages;
using Bus.Transport.Network;

namespace Tests.InfrastructureHandlers
{
    [TestFixture]
    public class CompletionMessageHandlerTests
    {
        private CompletionMessagesHandler _handler;
        private Mock<ICallbackRepository> _callbackManagerMock;

        [SetUp]
        public void setup()
        {
            _callbackManagerMock = new Mock<ICallbackRepository>();
            _handler = new CompletionMessagesHandler(_callbackManagerMock.Object);
        }

        [Test]
        public void should_invoke_proper_callback_when_receiving_message()
        {
            var messageId = Guid.NewGuid();
            var completionAcknowledgementMessage = new CompletionAcknowledgementMessage(messageId, "test", true, null);
            bool success = false;
            var completionCallbackMock = new Mock<ICompletionCallback>();
            completionCallbackMock.Setup(x => x.ExecuteCallback(completionAcknowledgementMessage)).Callback(() => success = true);
            _callbackManagerMock.Setup(x => x.GetCallback(messageId)).Returns(completionCallbackMock.Object);

            _handler.Handle(completionAcknowledgementMessage);

            _callbackManagerMock.Verify(x => x.GetCallback(It.IsAny<Guid>()), Times.Once());
            _callbackManagerMock.Verify(x => x.RemoveCallback(messageId), Times.Once());
            Assert.IsTrue(success);
        }
    }
}