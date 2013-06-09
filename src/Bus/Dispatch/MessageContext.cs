using System;
using Bus.Transport.Network;

namespace Bus.Dispatch
{
    public static class MessageContext
    {
        [ThreadStatic]
        private static PeerId _peerId;
        [ThreadStatic]
        private static IEndpoint _originatingEndpoint;

        public static PeerId PeerId
        {
            get { return _peerId; }
        }

        public static IEndpoint OriginatingEndpoint
        {
            get { return _originatingEndpoint; }
        }

        public static IDisposable SetContext(PeerId peerName, IEndpoint transportType)
        {
            return new Scope(peerName, transportType);
        }

        private class Scope :IDisposable
        {
            public Scope(PeerId peerId, IEndpoint endpoint)
            {
                _peerId = peerId;
                _originatingEndpoint = endpoint;
            }

            public void Dispose()
            {
                _peerId = null;
                _originatingEndpoint = null;
            }
        }
    }
}