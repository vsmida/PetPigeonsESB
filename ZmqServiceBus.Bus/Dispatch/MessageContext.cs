using System;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Dispatch
{
    public static class MessageContext
    {
        [ThreadStatic]
        private static string _peerName;
        [ThreadStatic]
        private static WireTransportType? _originatingTransportType;

        public static string PeerName
        {
            get { return _peerName; }
        }

        public static WireTransportType? OriginatingTransportType
        {
            get { return _originatingTransportType; }
        }

        public static IDisposable SetContext(string peerName, WireTransportType transportType)
        {
            return new Scope(peerName, transportType);
        }

        private class Scope :IDisposable
        {
            public Scope(string peerName, WireTransportType transportType)
            {
                _peerName = peerName;
                _originatingTransportType = transportType;
            }

            public void Dispose()
            {
                _peerName = null;
                _originatingTransportType = null;
            }
        }
    }
}