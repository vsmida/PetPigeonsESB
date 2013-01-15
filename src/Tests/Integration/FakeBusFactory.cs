using Bus;
using Bus.Dispatch;
using Bus.Startup;
using Bus.Transport;

namespace Tests.Integration
{
    public class FakeBusFactory
    {
        public static IBus CreateFakeBus(int busReceptionPort, string busName, int directoryServicePort, string directoryServiceName, IAssemblyScanner assemblyScanner = null, IPeerConfiguration peerconfig = null)
        {
            return BusFactory.CreateBus(containerConfigurationExpression: ctx =>
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
                                directoryServiceName

                        });

                ctx.For<IPeerConfiguration>().Use(
                   peerconfig ?? new DummyPeerConfig(busName, null));

                ctx.For<IAssemblyScanner>().Use(
                    assemblyScanner ?? new AssemblyScanner());
            });
        }

    }
}