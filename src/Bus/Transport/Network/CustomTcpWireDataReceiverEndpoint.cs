using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Shared;

namespace Bus.Transport.Network
{
    public class CustomTcpWireDataReceiverEndpoint : IEndpoint
    {
        public readonly IPEndPoint EndPoint;

        public CustomTcpWireDataReceiverEndpoint(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public bool Equals(CustomTcpWireDataReceiverEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(EndPoint, other.EndPoint);
        }

        public bool Equals(IEndpoint other)
        {
            return Equals((object)other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomTcpWireDataReceiverEndpoint) obj);
        }

        public override int GetHashCode()
        {
            return (EndPoint != null ? EndPoint.GetHashCode() : 0);
        }

        public static bool operator ==(CustomTcpWireDataReceiverEndpoint left, CustomTcpWireDataReceiverEndpoint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CustomTcpWireDataReceiverEndpoint left, CustomTcpWireDataReceiverEndpoint right)
        {
            return !Equals(left, right);
        }

        public WireTransportType WireTransportType { get{return WireTransportType.CustomTcpTransport;}}
        public bool IsMulticast { get { return false; } }
    }

    public class CustomTcpWireDataReceiverEndpointSerializer : EndpointSerializer<CustomTcpWireDataReceiverEndpoint>
    {
        public override Stream Serialize(CustomTcpWireDataReceiverEndpoint item)
        {
            var ipBuffer = item.EndPoint.Address.GetAddressBytes();
            var buffer = new byte[1 + 16 + 4];
            buffer[0] = ipBuffer.Length == 16 ? (byte)1 : (byte)0;
            for (int i = 0; i < 16; i++)
            {
                if (ipBuffer.Length > i)
                    buffer[i + 1] = ipBuffer[i];
                else
                    buffer[i+1] = 0;
            }
            ByteUtils.WriteInt(buffer, 17, item.EndPoint.Port);
            return new MemoryStream(buffer);
        }

        public override CustomTcpWireDataReceiverEndpoint Deserialize(Stream item)
        {
            var isIpV6 = Convert.ToBoolean(item.ReadByte());
            IPAddress address;
            if(isIpV6)
            {
                byte[] buff = new byte[16];
                address = new IPAddress(item.Read(buff, 0, 16));
            }
            else
            {
                byte[] buff = new byte[4];
                item.Read(buff, 0, 4);
                address = new IPAddress(buff);
                for (int i = 0; i < 12; i++) //skip padding
                {
                    item.ReadByte();
                }
            }
            var port = ByteUtils.ReadIntFromStream(item);

            return new CustomTcpWireDataReceiverEndpoint(new IPEndPoint(address, port));
        }
    }
}