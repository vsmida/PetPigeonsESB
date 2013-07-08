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
        private string _originalString = "Hello guys";
        private FrameAccumulator _frameAccumulator;
        private int _messagesReceived = 0;

        [SetUp]
        public void setup()
        {
            _frameAccumulator = new FrameAccumulator();
            _messagesReceived = 0;
        }

        [Test]
        public void should_handle_not_getting_full_length_2_buffers_with_offset_on_second()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            var secondBuffer = new byte[buffer.Length + 3];
            Array.Copy(lengthBuffer, 2, secondBuffer, 1, 2);
            Array.Copy(buffer, 0, secondBuffer, 3, buffer.Length);

            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(lengthBuffer, 0, 2);
            _frameAccumulator.AddFrame(secondBuffer, 1, secondBuffer.Length - 1);
            Assert.AreEqual(1, _messagesReceived);


        }

        [Test]
        public void should_handle_not_getting_full_length_2_buffers_with__no_offset_on_second()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var buffer2 = Encoding.ASCII.GetBytes("t");
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            var lengthBuffer2 = BitConverter.GetBytes(buffer2.Length);
            var secondBuffer = new byte[buffer.Length + 2 + 4 + buffer2.Length];
            Array.Copy(lengthBuffer, 2, secondBuffer, 0, 2);
            Array.Copy(buffer, 0, secondBuffer, 2, buffer.Length);
            Array.Copy(lengthBuffer2, 0, secondBuffer, buffer.Length +2, 4);
            Array.Copy(buffer2, 0, secondBuffer, buffer.Length + 2+4, buffer2.Length);



            _frameAccumulator.MessageReceived += stream =>
                                                     {
                                                         if(_messagesReceived == 0)
                                                         {
                                                             byte[] messageBuffer = new byte[stream.Length];
                                                             stream.Read(messageBuffer, 0, (int)stream.Length);
                                                             _messagesReceived++;
                                                             var stringMessage = Encoding.ASCII.GetString(messageBuffer);
                                                             Assert.AreEqual(_originalString, stringMessage); 
                                                         }
                                                         else
                                                         {
                                                             byte[] messageBuffer = new byte[stream.Length];
                                                             stream.Read(messageBuffer, 0, (int)stream.Length);
                                                             _messagesReceived++;
                                                             var stringMessage = Encoding.ASCII.GetString(messageBuffer);
                                                             Assert.AreEqual("t", stringMessage);
                                                         }

                                                     };
            _frameAccumulator.AddFrame(lengthBuffer, 0, 2);
            _frameAccumulator.AddFrame(secondBuffer, 0, secondBuffer.Length);
            Assert.AreEqual(2, _messagesReceived);


        }

        [Test]//todo
        public void should_handle_not_getting_full_length_twice()
        {
            var buffer = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            var secondBuffer = new byte[buffer.Length + 2];
            Array.Copy(lengthBuffer, 2, secondBuffer, 0, 2);
            Array.Copy(buffer, 0, secondBuffer, 2, buffer.Length);

            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(lengthBuffer, 0, 1);
            _frameAccumulator.AddFrame(lengthBuffer, 1, 1);
            _frameAccumulator.AddFrame(secondBuffer, 0, secondBuffer.Length);
            Assert.AreEqual(1, _messagesReceived);
        }


        [Test]
        public void should_be_able_to_return_multiple_messages_perfect_delimitation()
        {
            var bufferOrig = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(bufferOrig.Length);
            var buffer = new byte[32];

            Array.Copy(lengthBuffer, 0, buffer, 0, 4);
            Array.Copy(bufferOrig, 0, buffer, 4, 10);
            Array.Copy(lengthBuffer, 0, buffer, 14, 4);
            Array.Copy(bufferOrig, 0, buffer, 18, 10);



            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(buffer, 0, 28);
            Assert.AreEqual(2, _messagesReceived);

        }

        [Test]
        public void should_handle_getting_length_then_message()
        {
            var bufferOrig = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(bufferOrig.Length);
            var buffer = new byte[33];
            var buffer2 = new byte[11];

            Array.Copy(lengthBuffer, 0, buffer, 0, 4);
            Array.Copy(bufferOrig, 0, buffer, 4, bufferOrig.Length);
            Array.Copy(lengthBuffer, 0, buffer, 14, 4);
            Array.Copy(bufferOrig, 0, buffer2, 0, bufferOrig.Length);


            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(buffer, 0, 18);
            _frameAccumulator.AddFrame(buffer2, 0, 10);
            Assert.AreEqual(2, _messagesReceived);

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
        public void should_handle_getting_length_then_split_message()
        {
            var bufferOrig = Encoding.ASCII.GetBytes(_originalString);
            var lengthBuffer = BitConverter.GetBytes(bufferOrig.Length);
            var buffer = new byte[33];
            var buffer2 = new byte[11];

            Array.Copy(lengthBuffer, 0, buffer, 0, 4);
            Array.Copy(bufferOrig, 0, buffer, 4, bufferOrig.Length);
            Array.Copy(lengthBuffer, 0, buffer, 14, 4);
            Array.Copy(bufferOrig, 0, buffer, 18, bufferOrig.Length - 2);
            Array.Copy(bufferOrig, bufferOrig.Length - 2, buffer2, 0, 2);

            _frameAccumulator.MessageReceived += CheckMessageReceived;
            _frameAccumulator.AddFrame(buffer, 0, 18 + 8);
            _frameAccumulator.AddFrame(buffer2, 0, 2);
            Assert.AreEqual(2, _messagesReceived);

            _frameAccumulator.AddFrame(buffer, 0, 18 + 8);
            _frameAccumulator.AddFrame(buffer2, 0, 2);
            Assert.AreEqual(4, _messagesReceived);

        }




        [Test]
        public void should_handle_getting_message_too_big_for_spare_buffer()
        {
            _frameAccumulator = new FrameAccumulator();

            should_be_able_to_return_multiple_messages_perfect_delimitation();
            setup();
            _frameAccumulator = new FrameAccumulator();
            should_handle_getting_length_then_message();
            setup();
            _frameAccumulator = new FrameAccumulator();
            should_handle_getting_length_then_split_message();
            setup();
            _frameAccumulator = new FrameAccumulator();
            should_handle_not_getting_full_length_2_buffers_with_offset_on_second();
        }



    }
}