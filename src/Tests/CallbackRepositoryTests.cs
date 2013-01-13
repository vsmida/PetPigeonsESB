using System;
using Moq;
using NUnit.Framework;
using Bus;

namespace Tests
{
    [TestFixture]
    public class CallbackRepositoryTests
    {
        private CallbackRepository _callbackRepository;

        [SetUp]
        public void setup()
        {
            _callbackRepository = new CallbackRepository();
        }

        [Test]
        public void should_register_and_get_callback()
        {
            var completionCallbackMock = new Mock<ICompletionCallback>();
            var messageId = Guid.NewGuid();

            _callbackRepository.RegisterCallback(messageId, completionCallbackMock.Object);
            var callback = _callbackRepository.GetCallback(messageId);

            Assert.AreEqual(completionCallbackMock.Object, callback);
        }

        [Test]
        public void should_remove_callback()
        {
            var completionCallbackMock = new Mock<ICompletionCallback>();
            var messageId = Guid.NewGuid();

            _callbackRepository.RegisterCallback(messageId, completionCallbackMock.Object);
            var callback = _callbackRepository.GetCallback(messageId);

            Assert.AreEqual(completionCallbackMock.Object, callback);

            _callbackRepository.RemoveCallback(messageId);
            
            Assert.IsNull(_callbackRepository.GetCallback(messageId));
        }
    }
}