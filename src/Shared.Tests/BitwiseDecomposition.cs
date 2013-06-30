using System;
using System.Threading;
using NUnit.Framework;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class BitwiseDecomposition
    {
        [Test] public void should_fiddle_with_bits()
        {
            
                ushort indexFlag1 = 2;
                ushort userFlag1 = 3;
                ushort comittedFlag1 = 1;

                long longComposition = ((long)indexFlag1 << 48)  | ((long)userFlag1 << 32) | comittedFlag1;
                Console.WriteLine(Convert.ToString(longComposition, 2));
                var comitt2 = longComposition & (1);
                var userFlag2 = longComposition >> 32 & ushort.MaxValue;
                var indexFlag = longComposition >> 48 & ushort.MaxValue;

                Assert.AreEqual(comitt2, comittedFlag1);
                Assert.AreEqual(userFlag1, userFlag2);
                Assert.AreEqual(indexFlag, indexFlag1);

                var compComitted0 = longComposition - 1;
                Console.WriteLine(Convert.ToString(compComitted0, 2));
                comitt2 = compComitted0 & (1);
                userFlag2 = compComitted0 >> 32 & ushort.MaxValue;
                indexFlag = compComitted0 >> 48 & ushort.MaxValue;
                
                Assert.AreEqual(comitt2, 0);
                Assert.AreEqual(userFlag1, userFlag2);
                Assert.AreEqual(indexFlag, indexFlag1);

                var compuser15 = longComposition + (12L << 32);

                comitt2 = compuser15 & (1);
                userFlag2 = compuser15 >> 32 & ushort.MaxValue;
                indexFlag = compuser15 >> 48 & ushort.MaxValue;

                Assert.AreEqual(comitt2, comittedFlag1);
                Assert.AreEqual(userFlag2, 15);
                Assert.AreEqual(indexFlag, indexFlag1);

            

        }

        [Test]
        public void testInterlockedAdd()
        {
            var test = 2;
            var toto = Interlocked.Add(ref test, 1);
            Console.WriteLine(toto);
        }
    }
}