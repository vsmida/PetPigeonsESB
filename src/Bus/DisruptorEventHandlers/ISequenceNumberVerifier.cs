using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bus.InfrastructureMessages;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using log4net;

namespace Bus.DisruptorEventHandlers
{
    interface ISequenceNumberVerifier
    {
        bool IsSequenceNumberValid(ReceivedTransportMessage data, bool syncProcessorInitialized);
        void ResetSequenceNumbersForPeer(string peer);
    }

    class SequenceNumberVerifier : ISequenceNumberVerifier
    {

        private struct PeerTransportKey
        {
            public readonly string Peer;
            public readonly IEndpoint Endpoint;

            public PeerTransportKey(string peer, IEndpoint endpoint)
            {
                Peer = peer;
                Endpoint = endpoint;
            }

            public bool Equals(PeerTransportKey other)
            {
                return string.Equals(Peer, other.Peer) && Equals(Endpoint, other.Endpoint);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is PeerTransportKey && Equals((PeerTransportKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Peer != null ? Peer.GetHashCode() : 0) * 397) ^ (Endpoint != null ? Endpoint.GetHashCode() : 0);
                }
            }
        }


        private readonly Dictionary<PeerTransportKey, int> _sequenceNumber = new Dictionary<PeerTransportKey, int>();
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly ILog _logger = LogManager.GetLogger(typeof(SequenceNumberVerifier));

        public SequenceNumberVerifier(IPeerConfiguration peerConfiguration)
        {
            _peerConfiguration = peerConfiguration;
        }

        public bool IsSequenceNumberValid(ReceivedTransportMessage data, bool syncProcessorInitialized)
        {
            var transportMessageSequenceNumber = data.SequenceNumber;
            if (!transportMessageSequenceNumber.HasValue)
                return true;
            var peerKey = new PeerTransportKey(data.PeerName, data.Endpoint);
            int currentSeqNum;
            if (!_sequenceNumber.TryGetValue(peerKey, out currentSeqNum))
            {
                _sequenceNumber.Add(peerKey, -1);
                currentSeqNum = -1;
            }
            if (syncProcessorInitialized)
            {
                if (_peerConfiguration.PeerName != peerKey.Peer)
                {
                    if (transportMessageSequenceNumber != (currentSeqNum + 1))
                    {
                        _logger.Info(string.Format("missed message from endpoint {3} from peer {0} from sequence number {1} to {2}",
                                          peerKey.Peer,
                                          currentSeqNum + 1,
                                          transportMessageSequenceNumber,
                                          peerKey.Endpoint));
#if DEBUG
                        Debugger.Break();
                        //we missed a message
#endif
                        return false;
                    }

                    _sequenceNumber[peerKey] = transportMessageSequenceNumber.Value;
                }
            }
            else
            {
                _sequenceNumber[peerKey] = transportMessageSequenceNumber.Value;
            }
            return true;
        }

        public void ResetSequenceNumbersForPeer(string peer)
        {
            var keysToRemove = _sequenceNumber.Keys.Where(x => x.Peer == peer).ToList();
            foreach (var key in keysToRemove)
            {
                _sequenceNumber.Remove(key);
            }
        }
    }
}