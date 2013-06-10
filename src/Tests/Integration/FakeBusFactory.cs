using Bus;
using Bus.Dispatch;
using Bus.Startup;
using Bus.Transport;
using StructureMap;
using Tests.Transport;

namespace Tests.Integration
{
    public class FakeBusFactory
    {
        public static IBus CreateFakeBus(int busReceptionPort, string busName, PeerId busId, int directoryServicePort, string directoryServiceName,PeerId directoryServiceId, IAssemblyScanner assemblyScanner = null, IPeerConfiguration peerconfig = null, IContainer container = null)
        {
            container = container ?? new Container();
            return BusFactory.CreateBus(container, containerConfigurationExpression: ctx =>
            {
                ctx.For
                    <ZmqTransportConfiguration>()
                    .Use(
                        new DummyTransportConfig(
                            busReceptionPort));

                ctx.For
                    <IBusBootstrapperConfiguration
                        >().Use(new DummyBootstrapperConfig
                        {
                            DirectoryServiceEndpoint
                                =
                                "tcp://localhost:" +
                                directoryServicePort,
                            DirectoryServiceName
                                =
                                directoryServiceName,
                                DirectoryServiceId =  directoryServiceId

                        });

                ctx.For<IPeerConfiguration>().Use(
                   peerconfig ?? new DummyPeerConfig(busName,busId, null));

                ctx.For<IAssemblyScanner>().Use(
                    assemblyScanner ?? new AssemblyScanner());

            });
        }

    }
}