using System;
using System.Threading;
using NUnit.Framework;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class DefaultCompletionCallbackTests
    {
        private DefaultCompletionCallback _defaultCompletionCallback;

        [SetUp]
        public void setup()
        {
            _defaultCompletionCallback = new DefaultCompletionCallback();
        }

        [Test]
        public void should_wait_for_completion()
        {
            var thread = new Thread(() => _defaultCompletionCallback.WaitForCompletion());
            thread.Start();

            Assert.False(thread.Join(300));

            _defaultCompletionCallback.ExecuteCallback(new CompletionAcknowledgementMessage(Guid.NewGuid(),"test", true, WireTransportType.ZmqPushPullTransport));

            Assert.IsTrue(thread.Join(300));
            
        }

        [Test]
        public void should_execute_registered_callback()
        {
            bool called = false;

            _defaultCompletionCallback.RegisterCallback((mess) => called = true);
            _defaultCompletionCallback.ExecuteCallback(new CompletionAcknowledgementMessage(Guid.NewGuid(), "test", true, WireTransportType.ZmqPushPullTransport));

            Assert.IsTrue(called);
        }

        [Test]
        public void should_throw_when_message_processing_failed()
        {
            Assert.Throws<FailedMessageProcessingException>(() => _defaultCompletionCallback.ExecuteCallback(new CompletionAcknowledgementMessage(Guid.NewGuid(), "test", false, WireTransportType.ZmqPushPullTransport)));
            
        }
    }
}