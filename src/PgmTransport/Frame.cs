using System;
using Shared;

namespace PgmTransport
{
    struct Frame : IDisposable
    {
        private readonly Pool<byte[]> _bufferPool;

        public Frame(byte[] buffer, int offset, int count, Pool<byte[]> bufferPool = null)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
            _bufferPool = bufferPool;
        }

        public int Count;
        public int Offset;
        public byte[] Buffer;

        public void Dispose()
        {
            _bufferPool.PutBackItem(Buffer);
        }
    }
}