using System;
using System.Linq;
using Bus.BusEventProcessorCommands;
using Bus.InfrastructureMessages.Shadowing;
using Bus.InfrastructureMessages.Topology;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;

namespace Bus.Handlers
{
    class DirectoryServiceMessagesHandler : IBusEventHandler<PeerConnected>, ICommandHandler<InitializeTopologyAndMessageSettings>,
                                                   ICommandHandler<RegisterPeerCommand>, ICommandHandler<InitializeTopologyRequest>
    {
        private readonly IPeerManager _peerManager;
        private readonly IReplier _replier;
        private readonly IMessageOptionsRepository _optionsRepository;
        private readonly IDataReceiver _dataReceiver;
        private readonly IPeerConfiguration _peerConfiguration;

        public DirectoryServiceMessagesHandler(IPeerManager peerManager, IMessageOptionsRepository optionsRepository, IReplier replier, IDataReceiver dataReceiver, IPeerConfiguration peerConfiguration)
        {
            _peerManager = peerManager;
            _optionsRepository = optionsRepository;
            _replier = replier;
            _dataReceiver = dataReceiver;
            _peerConfiguration = peerConfiguration;
        }

        public void Handle(PeerConnected message)
        {
            _peerManager.RegisterPeerConnection(message.Peer);
            ResetInboundPeerSequenceNumbers(message);
           // PublishSavedMessages(message.Peer.PeerName);
           
        }

        private void ResetInboundPeerSequenceNumbers(PeerConnected message)
        {
            _dataReceiver.InjectCommand(new ResetSequenceNumbersForPeer(message.Peer.PeerName));
        }

        private void PublishSavedMessages(string peerName)
        {
            if (_peerConfiguration.ShadowedPeers != null && _peerConfiguration.ShadowedPeers.Contains(peerName))
            {
                var serializedData = BusSerializer.Serialize(new PublishUnacknowledgedMessagesToPeer(peerName));
                _dataReceiver.InjectMessage(new ReceivedTransportMessage(typeof(PublishUnacknowledgedMessagesToPeer).FullName,
                                                                         _peerConfiguration.PeerName,
                                                                         Guid.NewGuid(),
                                                                         null,
                                                                         serializedData, -1));
            }
        }

        public void Handle(InitializeTopologyAndMessageSettings message)
        {
            foreach (var servicePeer in message.KnownPeers)
            {
                _peerManager.RegisterPeerConnection(servicePeer);
          //      PublishSavedMessages(servicePeer.PeerName);
            }
            foreach (var messageOption in message.MessageOptions)
            {
                _optionsRepository.RegisterOptions(messageOption);
            }
        }

        public void Handle(RegisterPeerCommand item)
        {
            _peerManager.RegisterPeerConnection(item.Peer);

            //   _bus.Publish(new PeerConnected(item.Peer));
        }

        public void Handle(InitializeTopologyRequest item)
        {
            var initCommand = new InitializeTopologyAndMessageSettings(_peerManager.GetAllPeers().ToList(),
                                                                       _optionsRepository.GetAllOptions().Values.ToList());
            _peerManager.RegisterPeerConnection(item.Peer);

            _replier.Reply(initCommand);
        }
    }
}