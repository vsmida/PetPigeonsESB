using NUnit.Framework;
using Bus;

namespace Tests
{
    [TestFixture]
    public class BusCreationTest
    {

        [Test]
        public void should_create_bus()
        {
            Assert.DoesNotThrow(() => BusFactory.CreateBus());
        }
    }
}