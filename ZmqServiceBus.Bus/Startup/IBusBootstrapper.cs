using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using System.Linq;

namespace ZmqServiceBus.Bus.Startup
{
    public interface IBusBootstrapper
    {
        void BootStrapTopology();
    }

    public class BusBootstrapper : IBusBootstrapper
    {
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly ZmqTransportConfiguration _zmqTransportConfiguration;
        private readonly IBusBootstrapperConfiguration _bootstrapperConfiguration;
        private readonly IMessageOptionsRepository _optionsRepo;
        private readonly IMessageSender _messageSender;
        private readonly IPeerManager _peerManager;
        private readonly ISubscriptionManager _subscriptionManager;

        public BusBootstrapper(IAssemblyScanner assemblyScanner, ZmqTransportConfiguration zmqTransportConfiguration, IBusBootstrapperConfiguration bootstrapperConfiguration,
            IMessageOptionsRepository optionsRepo, IMessageSender messageSender, IPeerManager peerManager, ISubscriptionManager subscriptionManager)
        {
            _assemblyScanner = assemblyScanner;
            _zmqTransportConfiguration = zmqTransportConfiguration;
            _bootstrapperConfiguration = bootstrapperConfiguration;
            _optionsRepo = optionsRepo;
            _messageSender = messageSender;
            _peerManager = peerManager;
            _subscriptionManager = subscriptionManager;
        }

        public void BootStrapTopology()
        {
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(InitializeTopologyAndMessageSettings).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(RegisterPeerCommand).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(PeerConnected).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(CompletionAcknowledgementMessage).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));

           //var peer = new ServicePeer(_zmqTransportConfiguration.PeerName,
           //    _assemblyScanner.GetHandledCommands().ToDictionary(x => x, x => new ZmqEndpoint(_zmqTransportConfiguration.GetCommandsConnectEnpoint()) as IEndpoint)
           //    .Concat(_assemblyScanner.GetHandledEvents().ToDictionary(x => x, x => new ZmqEndpoint(_zmqTransportConfiguration.GetEventsConnectEndpoint()) as IEndpoint)
           //    ).ToDictionary(x => x.Key, x => x.Value));
        //    var command = new RegisterPeerCommand(peer);
      //      var directoryServiceBarebonesPeer = new ServicePeer(_bootstrapperConfiguration.DirectoryServiceName, _bootstrapperConfiguration.DirectoryServiceCommandEndpoint,
       //                                                         _bootstrapperConfiguration.DirectoryServiceEventEndpoint, new List<Type> { typeof(RegisterPeerCommand) });
        //    _peerManager.RegisterPeer(directoryServiceBarebonesPeer);

            foreach (var handledEvent in _assemblyScanner.GetHandledEvents())
            {
                _subscriptionManager.StartListeningTo(handledEvent);
            }

          //  _messageSender.Send(command).WaitForCompletion(); //now should get a init topo reply and the magic is done?
        }
    }
}