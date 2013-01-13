using Shared;

namespace Bus.Transport
{
    public interface IZmqTransportConfiguration
    {
        int Port { get; }
        string Protocol { get; }


    }

    public class ZmqTransportConfigurationRandomPort : ZmqTransportConfiguration
    {
        private readonly int _port;

        public ZmqTransportConfigurationRandomPort()
        {
            _port = NetworkUtils.GetRandomUnusedPort();
        }

        public override int Port
        {
            get { return _port; }
        }

        public override string Protocol
        {
            get { return "tcp"; }
        }
    }

    public abstract class ZmqTransportConfiguration : IZmqTransportConfiguration
    {
        public string GetBindEndpoint()
        {
            return Protocol + "://*:" + Port;
        }

        public string GetConnectEndpoint()
        {
            return Protocol + "://" + NetworkUtils.GetOwnIp() + ":" + Port;
        }

        public abstract int Port { get; }
        public abstract string Protocol { get; }

    }
}