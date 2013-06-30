using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Shared
{
    public unsafe class CustomSocket : Socket
    {
        public CustomSocket(SocketType socketType, ProtocolType protocolType)
            : base(socketType, protocolType)
        {
        }

        public CustomSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
        }

        public CustomSocket(SocketInformation socketInformation)
            : base(socketInformation)
        {

        }

        public int SendUnsafe(UnsafeNclNativeMethods.WSABuffer[] buffers, SocketFlags flags)
        {
            SocketError errorCode;
            int bytesTransferred;
            try
            {
                errorCode = UnsafeNclNativeMethods.OSSOCK.WSASend_Blocking(this.Handle,
                                                               buffers,
                                                               buffers.Length,
                                                               out bytesTransferred,
                                                               flags,
                                                               IntPtr.Zero,
                                                               IntPtr.Zero);

                if ((SocketError)errorCode == SocketError.SocketError)
                {
                    errorCode = (SocketError)Marshal.GetLastWin32Error();
                }
            }
            finally
            {

            }

            if (errorCode != SocketError.Success)
            {
                //
                // update our internal state after this socket error and throw 
                // 
                if (Connected && ((Handle.ToPointer() == null || Handle == new IntPtr(-1)) || (errorCode != SocketError.WouldBlock &&
                    errorCode != SocketError.IOPending && errorCode != SocketError.NoBufferSpaceAvailable)))
                {
                    // the socket is no longer a valid socket
                    // 
                    Disconnect(false);
                }
                return 0;
            }
            return bytesTransferred;

        }


    }
}