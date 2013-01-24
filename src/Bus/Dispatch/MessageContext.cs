using System;
using Bus.Transport.Network;

namespace Bus.Dispatch
{
    public static class MessageContext
    {
        [ThreadStatic]
        private static string _peerName;
        [ThreadStatic]
        private static IEndpoint _originatingEndpoint;

        public static string PeerName
        {
            get { return _peerName; }
        }

        public static IEndpoint OriginatingEndpoint
        {
            get { return _originatingEndpoint; }
        }

        public static IDisposable SetContext(string peerName, IEndpoint transportType)
        {
            return new Scope(peerName, transportType);
        }

        private class Scope :IDisposable
        {
            public Scope(string peerName, IEndpoint endpoint)
            {
                _peerName = peerName;
                _originatingEndpoint = endpoint;
            }

            public void Dispose()
            {
                _peerName = null;
                _originatingEndpoint = null;
            }
        }
    }
}