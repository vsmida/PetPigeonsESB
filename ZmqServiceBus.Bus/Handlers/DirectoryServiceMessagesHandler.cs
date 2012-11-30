using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Handlers
{
    public class DirectoryServiceMessagesHandler : IEventHandler<PeerConnected>, ICommandHandler<InitializeTopologyAndMessageSettings>
    {
        private readonly IPeerManager _peerManager;
        private readonly IMessageOptionsRepository _optionsRepository;

        public DirectoryServiceMessagesHandler(IPeerManager peerManager, IMessageOptionsRepository optionsRepository)
        {
            _peerManager = peerManager;
            _optionsRepository = optionsRepository;
        }

        public void Handle(PeerConnected message)
        {
            _peerManager.RegisterPeer(message.Peer);
        }

        public void Handle(InitializeTopologyAndMessageSettings message)
        {
            foreach (var servicePeer in message.KnownPeers)
            {
                _peerManager.RegisterPeer(servicePeer);
            }
            foreach (var messageOption in message.MessageOptions)
            {
                _optionsRepository.RegisterOptions(messageOption);
            }
        }
    }
}