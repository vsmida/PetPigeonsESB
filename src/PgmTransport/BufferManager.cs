using Shared;

namespace PgmTransport
{
    public class BufferManager
    {
        private readonly int _buffersSize;
        private Pool<byte[]> _pool;

        public BufferManager(int buffersSize, int initialCapacity)
        {
            _buffersSize = buffersSize;
            _pool = new Pool<byte[]>(() => new byte[_buffersSize], initialCapacity);

        }

        public byte[] GetBuffer()
        {
            return _pool.GetItem();
        }

        public void PutBackBuffer(byte[] buffer)
        {
            _pool.PutBackItem(buffer);
        }
    }
}