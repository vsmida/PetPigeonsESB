using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using PgmTransport;

namespace PgmTransportTests
{
    public class FrameAccumulatorTest
    {
        private PartialMessage _partialMessage;
        private string _originalString = "Hello guys";
        private FrameAccumulator _frameAccumulator;
        private int _messagesReceived = 0;

        [SetUp]
        public void setup()
        {
            _partialMessage = new PartialMessage();
            _frameAccumulator = new FrameAccumulator();
            _messagesReceived = 0;
        }

        [Test]
        public void should_handle_not_getting_full_length()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);

            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(new Frame(lengthBuffer, 0, 2));
            _frameAccumulator.AddFrame(new Frame(lengthBuffer, 2, 2));
            _frameAccumulator.AddFrame(new Frame(buffer, 0, buffer.Length));
            Assert.AreEqual(1, _messagesReceived);


        }
        
        [Test]
        public void should_be_able_to_return_multiple_messages()
        {
            var bufferOrig = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(bufferOrig.Length);
            var buffer = new byte[32];

            Array.Copy(lengthBuffer,0,buffer,0,4);
            Array.Copy(bufferOrig,0,buffer,4,10);
            Array.Copy(lengthBuffer, 0, buffer, 14, 4);
            Array.Copy(bufferOrig,0,buffer,18,10);

            Array.Copy(lengthBuffer, 0, buffer, 28, 4); //add garbage for next message


            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(new Frame(buffer, 0, 32));
            Assert.AreEqual(2, _messagesReceived);
            
        }

        [Test]
        public void should_be_able_to_return_multiple_messages_double_buffer()
        {
            var bufferOrig = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(bufferOrig.Length);
            var buffer = new byte[33];
            var buffer2 = new byte[11];

            Array.Copy(lengthBuffer, 0, buffer, 0, 4);
            Array.Copy(bufferOrig, 0, buffer, 4, 10);
            Array.Copy(lengthBuffer, 0, buffer, 14, 4);
            Array.Copy(bufferOrig, 0, buffer, 18, 10);

            Array.Copy(lengthBuffer, 0, buffer, 28, 4); 
            Array.Copy(bufferOrig, 0, buffer, 32, 1); 
            Array.Copy(bufferOrig, 1, buffer2, 0, 9); 



            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(new Frame(buffer, 0, 33));
            _frameAccumulator.AddFrame(new Frame(buffer2, 0, 9));
            Assert.AreEqual(3, _messagesReceived);

        }

        private void CheckMessageReceived(Stream stream)
        {

            byte[] messageBuffer = new byte[stream.Length];
            stream.Read(messageBuffer, 0, (int)stream.Length);
            _messagesReceived++;
            var stringMessage = Encoding.ASCII.GetString(messageBuffer);
            Assert.AreEqual(_originalString, stringMessage);
        }

        [Test]
        public void should_handle_getting_two_buffers()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);

            var b1 = new byte[10];
            var b2 = new byte[5];

            Array.Copy(buffer,0,b1,5,5);
            Array.Copy(buffer,5,b2,0,5);

            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            _frameAccumulator.MessageReceived += CheckMessageReceived;

            _frameAccumulator.AddFrame(new Frame(lengthBuffer, 0, 2));
            _frameAccumulator.AddFrame(new Frame(lengthBuffer, 2, 2));
            _frameAccumulator.AddFrame(new Frame(b1, 5, 5));
            _frameAccumulator.AddFrame(new Frame(b2, 0, b2.Length));

            Assert.AreEqual(1,_messagesReceived);

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


        //[Test]
        //public void accumulator_should_give_back_one_message()
        //{
        //    var buffer = Encoding.ASCII.GetBytes(_originalString);
        //    var lengthBuffer = BitConverter.GetBytes(buffer.Length);

        //    _frameAccumulator.AddFrame(new Frame(lengthBuffer, 0, 2));
        //    _frameAccumulator.AddFrame(new Frame(lengthBuffer, 2, 2));
        //    Assert.IsTrue(_frameAccumulator.AddFrame(new Frame(buffer, 0, buffer.Length)));

        //    var messages = _frameAccumulator.GetMessages();
        //    Assert.AreEqual(1, messages.Count);

        //    var message = messages.Dequeue();
        //    byte[] messageBuffer = new byte[buffer.Length];
        //    message.Read(messageBuffer, 0, (int)message.Length);

        //    var stringMessage = Encoding.ASCII.GetString(messageBuffer);
        //    Assert.AreEqual(_originalString, stringMessage);
        //}

        //[Test]
        //public void accumulator_should_give_back_multiple_message()
        //{

        //    var buffer = Encoding.ASCII.GetBytes(_originalString);
        //    var lengthBuffer = BitConverter.GetBytes(buffer.Length);
        //    var bigBuffer = new byte[8 + 2 * buffer.Length +3]; //+12 for simulating truncated message;
        //    Array.Copy(lengthBuffer, bigBuffer, 4);
        //    Array.Copy(buffer, 0, bigBuffer, 4, buffer.Length);
        //    Array.Copy(lengthBuffer, 0, bigBuffer, 4 + buffer.Length, 4);
        //    Array.Copy(buffer, 0, bigBuffer, 8 + buffer.Length, buffer.Length);

        //    Assert.IsTrue(_frameAccumulator.AddFrame(new Frame(bigBuffer, 0, bigBuffer.Length)));

        //    var messages = _frameAccumulator.GetMessages();
        //    Assert.AreEqual(2, messages.Count);

        //    var message = messages.Dequeue();
        //    byte[] messageBuffer = new byte[buffer.Length];
        //    message.Read(messageBuffer, 0, (int)message.Length);

        //    var stringMessage = Encoding.ASCII.GetString(messageBuffer);
        //    Assert.AreEqual(_originalString, stringMessage);

        //    message = messages.Dequeue();
        //     messageBuffer = new byte[buffer.Length];
        //    message.Read(messageBuffer, 0, (int)message.Length);

        //    stringMessage = Encoding.ASCII.GetString(messageBuffer);
        //    Assert.AreEqual(_originalString, stringMessage);

        //}

     
    }
}