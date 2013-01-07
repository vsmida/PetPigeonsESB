using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using System.Linq;

namespace ZmqServiceBus.Bus.Handlers
{
    public class DirectoryServiceMessagesHandler : IBusEventHandler<PeerConnected>, ICommandHandler<InitializeTopologyAndMessageSettings>,
                                                   ICommandHandler<RegisterPeerCommand>, ICommandHandler<InitializeTopologyRequest>
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
            _peerManager.RegisterPeerConnection(message.Peer);
        }

        public void Handle(InitializeTopologyAndMessageSettings message)
        {
            foreach (var servicePeer in message.KnownPeers)
            {
                _peerManager.RegisterPeerConnection(servicePeer);
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
                                                                       _optionsRepository.GetAllOptions());
            _peerManager.RegisterPeerConnection(item.Peer);

            _replier.Reply(initCommand);
        }
    }
}