using System;
using System.ComponentModel;
using System.Text;
using NUnit.Framework;
using PgmTransport;

namespace PgmTransportTests
{
    public class FrameAccumulatorTest
    {
        private PartialMessage _partialMessage;
        private string _originalString = "Hello guys";
        private FrameAccumulator _frameAccumulator;

        [SetUp]
        public void setup()
        {
            _partialMessage = new PartialMessage();
            _frameAccumulator = new FrameAccumulator();
        }

        [Test]
        public void should_handle_not_getting_full_length()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);

            _partialMessage.AddFrame(new Frame(lengthBuffer, 0, 2));
            _partialMessage.AddFrame(new Frame(lengthBuffer, 2, 2));
            _partialMessage.AddFrame(new Frame(buffer, 0, buffer.Length));

            CheckPartialMessage(buffer);
        }

        private void CheckPartialMessage(byte[] buffer)
        {
            Assert.IsTrue(_partialMessage.Ready);
            var messages = _partialMessage.GetMessage();

            byte[] messageBuffer = new byte[buffer.Length];
            messages.Read(messageBuffer, 0, (int) messages.Length);

            var stringMessage = Encoding.ASCII.GetString(messageBuffer);
            Assert.AreEqual(_originalString, stringMessage);
        }

        [Test]
        public void should_handle_getting_more_data_than_needed()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            var bigBuffer = new byte[4 + 2 * buffer.Length];
            Array.Copy(lengthBuffer, bigBuffer, 4);
            Array.Copy(buffer, 0, bigBuffer, 4, buffer.Length);
            Array.Copy(buffer, 0, bigBuffer, 4 + buffer.Length, buffer.Length);

            Assert.AreEqual(4 + buffer.Length, _partialMessage.AddFrame(new Frame(bigBuffer, 0, bigBuffer.Length)));

            CheckPartialMessage(buffer);
        }

        [Test]
        public void should_handle_being_cleared()
        {

            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);

            _partialMessage.AddFrame(new Frame(lengthBuffer, 0, 2));
            _partialMessage.AddFrame(new Frame(lengthBuffer, 2, 2));
            _partialMessage.AddFrame(new Frame(buffer, 0, buffer.Length));

            CheckPartialMessage(buffer);

            _partialMessage.Clear();

            _partialMessage.AddFrame(new Frame(lengthBuffer, 0, 2));
            _partialMessage.AddFrame(new Frame(lengthBuffer, 2, 2));
            _partialMessage.AddFrame(new Frame(buffer, 0, buffer.Length));

            CheckPartialMessage(buffer);

        }


        [Test]
        public void accumulator_should_give_back_one_message()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);

            _frameAccumulator.AddFrame(new Frame(lengthBuffer, 0, 2));
            _frameAccumulator.AddFrame(new Frame(lengthBuffer, 2, 2));
            Assert.IsTrue(_frameAccumulator.AddFrame(new Frame(buffer, 0, buffer.Length)));

            var messages = _frameAccumulator.GetMessages();
            Assert.AreEqual(1, messages.Count);

            var message = messages.Dequeue();
            byte[] messageBuffer = new byte[buffer.Length];
            message.Read(messageBuffer, 0, (int)message.Length);

            var stringMessage = Encoding.ASCII.GetString(messageBuffer);
            Assert.AreEqual(_originalString, stringMessage);
        }

        [Test]
        public void accumulator_should_give_back_multiple_message()
        {

            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            var bigBuffer = new byte[8 + 2 * buffer.Length +3]; //+12 for simulating truncated message;
            Array.Copy(lengthBuffer, bigBuffer, 4);
            Array.Copy(buffer, 0, bigBuffer, 4, buffer.Length);
            Array.Copy(lengthBuffer, 0, bigBuffer, 4 + buffer.Length, 4);
            Array.Copy(buffer, 0, bigBuffer, 8 + buffer.Length, buffer.Length);

            Assert.IsTrue(_frameAccumulator.AddFrame(new Frame(bigBuffer, 0, bigBuffer.Length)));

            var messages = _frameAccumulator.GetMessages();
            Assert.AreEqual(2, messages.Count);

            var message = messages.Dequeue();
            byte[] messageBuffer = new byte[buffer.Length];
            message.Read(messageBuffer, 0, (int)message.Length);

            var stringMessage = Encoding.ASCII.GetString(messageBuffer);
            Assert.AreEqual(_originalString, stringMessage);

            message = messages.Dequeue();
             messageBuffer = new byte[buffer.Length];
            message.Read(messageBuffer, 0, (int)message.Length);

            stringMessage = Encoding.ASCII.GetString(messageBuffer);
            Assert.AreEqual(_originalString, stringMessage);

        }

     
    }
}