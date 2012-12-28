using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using System.Linq;

namespace ZmqServiceBus.Bus.Handlers
{
    public class DirectoryServiceMessagesHandler : IEventHandler<PeerConnected>, ICommandHandler<InitializeTopologyAndMessageSettings>,
                                                   ICommandHandler<RegisterPeerCommand>
    {
        private readonly IPeerManager _peerManager;
        private readonly IBus _bus;
        private readonly IReplier _replier;
        private readonly IMessageOptionsRepository _optionsRepository;

        public DirectoryServiceMessagesHandler(IPeerManager peerManager, IMessageOptionsRepository optionsRepository, IReplier replier, IBus bus)
        {
            _peerManager = peerManager;
            _optionsRepository = optionsRepository;
            _replier = replier;
            _bus = bus;
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

        public void Handle(RegisterPeerCommand item)
        {
            _peerManager.RegisterPeer(item.Peer);

            var initCommand = new InitializeTopologyAndMessageSettings(_peerManager.GetAllPeers().ToList(),
                                                                       _optionsRepository.GetAllOptions());
            _replier.Reply(initCommand);

            _bus.Publish(new PeerConnected(item.Peer));
        }
    }
}