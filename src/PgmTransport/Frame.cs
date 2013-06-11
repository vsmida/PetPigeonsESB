using System;
using Shared;

namespace PgmTransport
{
    public struct Frame : IDisposable
    {
        public readonly Pool<byte[]> BufferPool;

        public Frame(byte[] buffer, int offset, int count, Pool<byte[]> bufferPool = null)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
            BufferPool = bufferPool;
        }

        public readonly int Count;
        public readonly int Offset;
        public byte[] Buffer;

        public void Dispose()
        {
            if(Offset+Count == Buffer.Length)//todo: awful trick because only disposed as "chunck" frames and not by socket.receive method who also has offset+count=length because it might give whole buffer
            if(BufferPool != null) //cannot release, who is still using it?
            BufferPool.PutBackItem(Buffer);
        }
    }
}