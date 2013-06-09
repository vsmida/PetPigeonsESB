using System;
using ProtoBuf;

namespace Bus
{
    [ProtoContract]
    public class PeerId : IEquatable<PeerId>
    {
        [ProtoMember(1, IsRequired = true)] public readonly int Id;

        public PeerId(int peerId)
        {
            Id = peerId;
        }
        private PeerId(){}

        public bool Equals(PeerId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PeerId) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(PeerId left, PeerId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PeerId left, PeerId right)
        {
            return !Equals(left, right);
        }
    }
}