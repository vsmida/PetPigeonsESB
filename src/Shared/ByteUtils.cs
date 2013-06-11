using System;
using System.IO;
using System.Text;

namespace Shared
{
    public static class ByteUtils
    {
        public static int ReadIntFromStream(Stream stream)
        {
            //var buffer = new byte[4];
            //stream.Read(buffer, 0, 4);

            //var b1 = buffer[0];
            //var b2 = buffer[1];
            //var b3 = buffer[2];
            //var b4 = buffer[3];

            var b1 = stream.ReadByte();
            var b2 = stream.ReadByte();
            var b3 = stream.ReadByte();
            var b4 = stream.ReadByte();
            var first = b1 << 24;
            var second = b2 << 16;
            var third = b3 << 8;
            var fourth = b4;

            return first + second + third + fourth;
        }

        public static int ReadInt(byte[] buffer, int offset)
        {
            var firstByte = buffer[offset];
            var first = firstByte << 24;
            var second = buffer[offset+1] << 16;
            var third = buffer[offset+2] << 8;
            var fourth = buffer[offset+3];

            return first + second + third + fourth;
        }

        public static void WriteInt(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        public static string ReadAsciiStringFromArray(byte[] array, int offset, int length)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                builder.Append((char)array[offset + i]);
            }
            return builder.ToString(); 
        }
        public static string ReadAsciiStringFromStream(Stream data, int messageTypeLength)
        {
           var builder = new StringBuilder();
           
            for (int i = 0; i < messageTypeLength; i++)
            {
                builder.Append((char)data.ReadByte());
            }
            return builder.ToString();
        }
    }
}