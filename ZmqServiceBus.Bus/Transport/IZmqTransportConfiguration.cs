using System.Configuration;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IZmqTransportConfiguration
    {
        int Port { get; }
        string Protocol { get; }
        string PeerName { get; }


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

        public override string PeerName
        {
            get { return ConfigurationManager.AppSettings["ServiceName"]; }
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
        public abstract string PeerName { get; }

    }
}