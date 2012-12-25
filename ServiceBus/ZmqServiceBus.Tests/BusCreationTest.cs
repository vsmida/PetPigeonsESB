using NUnit.Framework;
using StructureMap.Configuration.DSL;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Startup;
using ObjectFactory = StructureMap.ObjectFactory;

namespace ZmqServiceBus.Tests
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