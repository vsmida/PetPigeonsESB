using System;
using System.IO;
using System.Text;

namespace Shared
{
    public static class ByteUtils
    {
        public static int ReadIntFromStream(Stream stream)
        {
            if (BitConverter.IsLittleEndian)
                return stream.ReadByte() | stream.ReadByte() << 8 | stream.ReadByte() << 16 | stream.ReadByte() << 24;
            else
                return stream.ReadByte() << 24 | stream.ReadByte() << 16 | stream.ReadByte() << 8 | stream.ReadByte();
        }

        public unsafe static int ReadInt(byte[] buffer, int offset)
        {
            //var firstByte = buffer[offset];
            //var first = firstByte << 24;
            //var second = buffer[offset+1] << 16;
            //var third = buffer[offset+2] << 8;
            //var fourth = buffer[offset+3];

            fixed (byte* numPtr = &buffer[offset])
            {
                if ((offset & (4-1)) == 0)
                    return *(int*)numPtr;
                if (BitConverter.IsLittleEndian)
                    return (int)*numPtr | (int)numPtr[1] << 8 | (int)numPtr[2] << 16 | (int)numPtr[3] << 24;
                else
                    return (int)*numPtr << 24 | (int)numPtr[1] << 16 | (int)numPtr[2] << 8 | (int)numPtr[3];
            }


            //    return buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3];
        }

        public unsafe static void WriteInt(byte[] buffer, int offset, int value)
        {
            fixed (byte* numPtr = buffer)
                *(int*)(numPtr + offset) = value;
            //buffer[offset] = (byte)(value >> 24);
            //buffer[offset + 1] = (byte)(value >> 16);
            //buffer[offset + 2] = (byte)(value >> 8);
            //buffer[offset + 3] = (byte)value;
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