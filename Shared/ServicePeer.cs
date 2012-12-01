using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Shared
{
    public interface IServicePeer
    {
        string PeerName { get; }
        string ReceptionEndpoint { get; }
        string PublicationEndpoint { get; }

        List<Type> HandledMessages { get; }
        List<Type> PublishedMessages { get; }
    }

    [ProtoContract]
    public class ServicePeer : IServicePeer
    {
        [ProtoMember(1, IsRequired = true)]
        public string PeerName { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public string ReceptionEndpoint { get; set; }
        [ProtoMember(3, IsRequired = true)]
        public string PublicationEndpoint { get; set; }
        [ProtoMember(4, IsRequired = true)]
        private List<Type> _handledMessages;
        [ProtoMember(5, IsRequired = true)]
        private List<Type> _publishedMessages;
        public List<Type> HandledMessages
        {
            get { return _handledMessages; }
        }

        public List<Type> PublishedMessages
        {
            get { return _publishedMessages; }
        }

        public ServicePeer(string peerName, string receptionEndpoint, string publicationEndpoint, List<Type> handledMessages, List<Type> publishedMessages)
        {
            PeerName = peerName;
            ReceptionEndpoint = receptionEndpoint;
            PublicationEndpoint = publicationEndpoint;
            _handledMessages = handledMessages;
            _publishedMessages = publishedMessages;
        }

        private ServicePeer()
        {}
    }
}