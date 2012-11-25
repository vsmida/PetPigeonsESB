using NUnit.Framework;
using StructureMap.Configuration.DSL;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Startup;
using ObjectFactory = StructureMap.ObjectFactory;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class BusRegistryTests
    {
        [Test]
        public void should_be_able_to_instantiate_bus()
        {

            ObjectFactory.Initialize(x => x.AddRegistry<BusRegistry>());
            Assert.DoesNotThrow(() =>ObjectFactory.GetInstance<IBus>());
        }
    }
}