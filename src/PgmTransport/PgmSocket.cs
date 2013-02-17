using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using log4net;

namespace PgmTransport
{
    public class PgmSocket : Socket
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PgmSocket));

        public const int PROTOCOL_TYPE_NUMBER = 113;
        public const ProtocolType PGM_PROTOCOL_TYPE = (ProtocolType) 113;
        public const SocketOptionLevel PGM_LEVEL = (SocketOptionLevel) PROTOCOL_TYPE_NUMBER;

        public PgmSocket()
            : base(AddressFamily.InterNetwork, SocketType.Rdm, PGM_PROTOCOL_TYPE)
        {
        }

        public static void SetPgmOption(Socket socket, int option, byte[] value)
        {
            socket.SetSocketOption(PGM_LEVEL, (SocketOptionName)option, value);
        }

        public void SetPgmOption(int option, byte[] value)
        {
            try
            {
                SetSocketOption(PGM_LEVEL, (SocketOptionName)option, value);
            }
            catch (Exception failed)
            {
                log.Warn("failed", failed);
            }
        }

        public bool EnableGigabit()
        {
            return SetSocketOption(this, "Gigabit", 1014, 1);
        }

        public static bool SetSocketOption(Socket socket, string name, int option, uint val)
        {
            try
            {
                byte[] bits = BitConverter.GetBytes(val);
                SetPgmOption(socket, option, bits);
                log.Info("Set: " + name + " Option : " + option + " value: " + val);
                return true;
            }
            catch (Exception failed)
            {
                log.Debug(name + " Option : " + option + " value: " + val, failed);
                return false;
            }
        }

        public unsafe _RM_RECEIVER_STATS GetReceiverStats(Socket socket)
        {
            int size = sizeof(_RM_RECEIVER_STATS);
            byte[] data = socket.GetSocketOption(PGM_LEVEL, (SocketOptionName)1013, size);
            fixed (byte* pBytes = &data[0])
            {
                return *((_RM_RECEIVER_STATS*)pBytes);
            }
        }

        public static byte[] ConvertStructToBytes(object obj)
        {
            int structSize = Marshal.SizeOf(obj);
            byte[] allData = new byte[structSize];
            GCHandle handle = GCHandle.Alloc(allData, GCHandleType.Pinned);
            Marshal.StructureToPtr(obj, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return allData;
        }


        public void SetSendWindow(_RM_SEND_WINDOW window)
        {
          byte[] allData = PgmSocket.ConvertStructToBytes(window);
          SetSocketOption(PgmSocket.PGM_LEVEL, (SocketOptionName)1001, allData);
        }
    }
}