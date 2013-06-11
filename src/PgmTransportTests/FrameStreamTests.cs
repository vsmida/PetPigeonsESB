using System;
using System.Collections.Generic;
using NUnit.Framework;
using PgmTransport;

namespace PgmTransportTests
{
    [TestFixture]
    public class FrameStreamTests
    {
        private FrameStream _stream;

        [SetUp]
        public void setup()
        {
            _stream = new FrameStream();
        }


        [Test]
        public void should_handle_2_buffers_with_offsets_data_separate()
        {
            var data1 = Guid.NewGuid().ToByteArray();
            var data2 = Guid.NewGuid().ToByteArray();

            var buffer1 = new byte[2000];
            var buffer2 = new byte[2000];
            var offset1 = buffer1.Length - data1.Length; // end of buffer
            var offset2 = 0;
            Array.Copy(data1, 0, buffer1, offset1, data1.Length);
            Array.Copy(data2, 0, buffer2, offset2, data2.Length);

            var frame1 = new Frame(buffer1, offset1, data1.Length);
            var frame2 = new Frame(buffer2, offset2, data2.Length);

            _stream.SetFrames(new List<Frame> { frame1, frame2 });

            Assert.AreEqual(32, _stream.Length);

            var resultBUffer = new byte[16];
            _stream.Read(resultBUffer, 0, 16);
            Assert.AreEqual(resultBUffer,data1);
            _stream.Read(resultBUffer, 0, 16);
            Assert.AreEqual(resultBUffer, data2);


        }


        [Test]
        public void should_handle_2_buffers_with_offsets_data_overlapping()
        {
            var data1 = Guid.NewGuid().ToByteArray();
            var data2 = Guid.NewGuid().ToByteArray();

            var buffer1 = new byte[200];
            var buffer2 = new byte[200];
            var offset1 = buffer1.Length - data1.Length+4; 
            var offset2 = 0;
            Array.Copy(data1, 0, buffer1, offset1, data1.Length - 4);
            Array.Copy(data1, 12, buffer2, offset2, 4);
            Array.Copy(data2, 0, buffer2, offset2 + 4, data2.Length);

            var frame1 = new Frame(buffer1, offset1, 12);
            var frame2 = new Frame(buffer2, offset2, 20);

            _stream.SetFrames(new List<Frame> { frame1, frame2 });

            Assert.AreEqual(32, _stream.Length);

            var resultBUffer = new byte[16];
            _stream.Read(resultBUffer, 0, 16);
            Assert.AreEqual(resultBUffer, data1);
            _stream.Read(resultBUffer, 0, 16);
            Assert.AreEqual(resultBUffer, data2);


        }


        [Test]
        public void should_handle_2_buffers_with_offsets_data_overlapping_read_byte()
        {
            var data1 = Guid.NewGuid().ToByteArray();
            var data2 = Guid.NewGuid().ToByteArray();

            var buffer1 = new byte[200];
            var buffer2 = new byte[200];
            var offset1 = buffer1.Length - data1.Length + 4;
            var offset2 = 0;
            Array.Copy(data1, 0, buffer1, offset1, data1.Length - 4);
            Array.Copy(data1, 12, buffer2, offset2, 4);
            Array.Copy(data2, 0, buffer2, offset2 + 4, data2.Length);

            var frame1 = new Frame(buffer1, offset1, 12);
            var frame2 = new Frame(buffer2, offset2, 20);

            _stream.SetFrames(new List<Frame> { frame1, frame2 });

            Assert.AreEqual(32, _stream.Length);

            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(data1[i],(byte)_stream.ReadByte());
            }

            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(data2[i], (byte)_stream.ReadByte());

            }



        }
    }
}