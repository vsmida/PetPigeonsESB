using System;

namespace ZmqServiceBus.Bus.Dispatch
{
    public static class MessageContext
    {
        [ThreadStatic]
        private static string _peerName;

        public static string PeerName
        {
            get { return _peerName; }
        }

        public static IDisposable SetContext(string peerName)
        {
            return new Scope(peerName);
        }

        private class Scope :IDisposable
        {
            public Scope(string peerName)
            {
                _peerName = peerName;
            }

            public void Dispose()
            {
                _peerName = null;
            }
        }
    }
}