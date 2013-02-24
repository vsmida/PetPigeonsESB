using System;
using Shared;

namespace PgmTransport
{
    struct Frame : IDisposable
    {
        public readonly Pool<byte[]> BufferPool;

        public Frame(byte[] buffer, int offset, int count, Pool<byte[]> bufferPool = null)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
            BufferPool = bufferPool;
        }

        public int Count;
        public int Offset;
        public byte[] Buffer;

        public void Dispose()
        {
            if(BufferPool != null)
            BufferPool.PutBackItem(Buffer);
        }
    }
}