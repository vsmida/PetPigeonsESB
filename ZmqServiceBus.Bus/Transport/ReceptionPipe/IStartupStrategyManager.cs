using System;
using System.Collections.Generic;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface IStartupStrategyManager
    {
        IEnumerable<ReceivedTransportMessage> CheckMessage(IReceivedTransportMessage transportMessage);
        void RegisterStrategy(IStartupReliabilityStrategy strategy);
    }

    public class StartupStrategyManager : IStartupStrategyManager
    {
        private class StartUpKey
        {
            public string PeerName { get; set; }
            public string MessageType { get; set; }

            public StartUpKey(string peerName, string messageType)
            {
                PeerName = peerName;
                MessageType = messageType;
            }

            protected bool Equals(StartUpKey other)
            {
                return String.Equals(PeerName, (string) other.PeerName) && String.Equals(MessageType, (string) other.MessageType);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((StartUpKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((PeerName != null ? PeerName.GetHashCode() : 0) * 397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
                }
            }
        }

        public IEnumerable<ReceivedTransportMessage> CheckMessage(IReceivedTransportMessage transportMessage)
        {
            throw new NotImplementedException();
        }

        public void RegisterStrategy(IStartupReliabilityStrategy strategy)
        {
            throw new NotImplementedException();
        }
    }
}