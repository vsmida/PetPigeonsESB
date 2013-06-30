using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;

namespace Shared
{
    [SuppressUnmanagedCodeSecurity]
    public static class UnsafeNclNativeMethods
    {
        public struct WSABuffer
        {
            public int Length;
            public IntPtr Pointer;
        }
        
        [SuppressUnmanagedCodeSecurity]
        public static class OSSOCK
        {
            [DllImport("ws2_32.dll", EntryPoint = "WSASend", SetLastError = true)]
            public static extern SocketError WSASend_Blocking([In] IntPtr socketHandle, [In] WSABuffer[] buffersArray, [In] int bufferCount, out int bytesTransferred, [In] SocketFlags socketFlags, [In] IntPtr overlapped, [In] IntPtr completionRoutine);
            


        }


        //socket code

//        public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
//        {
//            if (s_LoggingEnabled) Logging.Enter(Logging.Sockets, this, "Send", "");
//            if (CleanedUp)
//            {
//                throw new ObjectDisposedException(this.GetType().FullName);
//            }
//            if (buffers == null)
//            {
//                throw new ArgumentNullException("buffers");
//            }

//            if (buffers.Count == 0)
//            {
//                throw new ArgumentException(SR.GetString(SR.net_sockets_zerolist, "buffers"), "buffers");
//            }

//            ValidateBlockingMode();
//            GlobalLog.Print("Socket#" + ValidationHelper.HashString(this) + "::Send() SRC:" + ValidationHelper.ToString(LocalEndPoint) + " DST:" + ValidationHelper.ToString(RemoteEndPoint));

//            //make sure we don't let the app mess up the buffer array enough to cause
//            //corruption.

//            errorCode = SocketError.Success;
//            int count = buffers.Count;
//            WSABuffer[] WSABuffers = new WSABuffer[count];
//            GCHandle[] objectsToPin = null;
//            int bytesTransferred;

//            try
//            {
//                objectsToPin = new GCHandle[count];
//                for (int i = 0; i < count; ++i)
//                {
//                    ArraySegment<byte> buffer = buffers[i];
//                    objectsToPin[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
//                    WSABuffers[i].Length = buffer.Count;
//                    WSABuffers[i].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset);
//                }

//                // This may throw ObjectDisposedException.
//                errorCode = UnsafeNclNativeMethods.OSSOCK.WSASend_Blocking(
//                    m_Handle.DangerousGetHandle(),
//                    WSABuffers,
//                    count,
//                    out bytesTransferred,
//                    socketFlags,
//                    IntPtr.Zero,
//                    IntPtr.Zero);

//                if ((SocketError)errorCode == SocketError.SocketError)
//                {
//                    errorCode = (SocketError)Marshal.GetLastWin32Error();
//                }

//            }
//            finally
//            {
//                if (objectsToPin != null)
//                    for (int i = 0; i < objectsToPin.Length; ++i)
//                        if (objectsToPin[i].IsAllocated)
//                            objectsToPin[i].Free();
//            }

//            if (errorCode != SocketError.Success)
//            {
//                //
//                // update our internal state after this socket error and throw 
//                // 
//                UpdateStatusAfterSocketError(errorCode);
//                if (s_LoggingEnabled)
//                {
//                    Logging.Exception(Logging.Sockets, this, "Send", new SocketException(errorCode));
//                    Logging.Exit(Logging.Sockets, this, "Send", 0);
//                }
//                return 0;
//            }
//            return bytesTransferred;
//        }

 

    }
}
