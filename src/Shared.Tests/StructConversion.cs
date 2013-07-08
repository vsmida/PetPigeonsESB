using System;
using NUnit.Framework;
using Shared;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class StructConversion_
    {

        [Test]
        public unsafe void should_convert_between_similar_structs()
        {
            var byteArray = new byte[3];
            byteArray[0] = 2;
            byteArray[1] = 99;
            byteArray[2] = 199;
            var arraySegmentArray = new ArraySegment<byte>[1];
            arraySegmentArray[0] = new ArraySegment<byte>(byteArray, 1, 2);

            var converter = new MutableToImmutableArraySegmentArrayConverter { ImmutableArray = arraySegmentArray };

            MutableArraySegment<byte> mutableArraySegment = converter.MutableArray[0];
            Assert.AreEqual(2, mutableArraySegment.Count);
            Assert.AreEqual(1, mutableArraySegment.Offset);
            var byteArrayMutable = mutableArraySegment.Array;
            Assert.AreEqual(byteArray[0], byteArrayMutable[0]);
            Assert.AreEqual(byteArray[1], byteArrayMutable[1]);
            Assert.AreEqual(byteArray[2], byteArrayMutable[2]);

            var mutable =
            new MutableArraySegment<byte>[1];

            Array.Copy(converter.MutableArray, 0, mutable, 0, 1);

        }
    }
}