using System;
using System.IO;

namespace ZmqServiceBus.Bus.Conventions
{
    public static class SocketIdentityConvention
    {
         public static byte[] GetIdentityFromConnectEndpoint(string endpoint)
         {
             if(endpoint.Contains("*"))
                 throw new ArgumentException("Endpoint string is not valid, are you providing a bind enpoint?");
             using (var stream = new MemoryStream())
             using (var writer = new BinaryWriter(stream))
             {
                 writer.Write(endpoint.GetHashCode());
                 return stream.ToArray();
             }
         }
    }
}