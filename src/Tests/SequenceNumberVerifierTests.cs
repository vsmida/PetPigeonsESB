using System;
using Bus;
using Bus.DisruptorEventHandlers;
using Bus.Transport.ReceptionPipe;
using NUnit.Framework;
using Tests.Integration;
using Tests.Transport;

namespace Tests
{
    [TestFixture]
    public class SequenceNumberVerifierTests
    {
        private SequenceNumberVerifier _verifier;
        [SetUp]
        public void setup()
        {
            _verifier = new SequenceNumberVerifier(new DummyPeerConfig("Me", null));
        }

        [Test]
        public void should_start_at_0_and_accept_first_message_when_initialized()
        {
           var valid = _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(), true);
            Assert.IsTrue(valid);
        }

        [Test]
        public void should_refuse_message_when_sequence_not_equal_to_previous_plus_one_when_initialized()
        {
            _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(), true);
            var valid = _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(), true);
            Assert.IsFalse(valid);
        }
        
        [Test]
        public void should_keep_track_of_previous_number_and_accept_only_if_next_number_is_previous_plus_one()
        {
            _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(Guid.NewGuid(), 0), true);
            Assert.IsTrue(_verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(Guid.NewGuid(),1), true));
            Assert.IsFalse(_verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(Guid.NewGuid(),3), true));
        }

        [Test]
        public void should_return_true_when_replaying_synchronization_messages() //we may have a sequence like 1-2-3-4-0-1-2-3 because of reconnects, todo: be more stringent and not return true all the time
        {
            _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(), true);
            var valid = _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(), false);
            Assert.IsTrue(valid);    
        }

        [Test]
        public void should_return_true_if_peer_is_myself()//todo:revise this
        {
            _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(Guid.NewGuid(),0, "Me"), true);
            var valid = _verifier.IsSequenceNumberValid(TestData.GenerateDummyReceivedMessage<TestData.FakeCommand>(Guid.NewGuid(), -1, "Me"), true);
            Assert.IsTrue(valid); 
        }


    }
}