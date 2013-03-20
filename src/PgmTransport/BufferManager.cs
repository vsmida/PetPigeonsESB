using System;
using System.ComponentModel;
using System.Threading;
using Shared;

namespace PgmTransport
{
    public class BufferManager
    {
        private readonly byte[] _bytes;
        private int _headerSize = 5;
        private const byte FreeBlock = 1;
        private const byte OccupiedBlock = 2;

        public BufferManager(int totalCapacity, int quantumSize)
        {
            _bytes = new byte[totalCapacity];
            ByteUtils.WriteInt(_bytes, 0, totalCapacity - 4);
            _bytes[_headerSize] = 1;
        }

        public ArraySegment<byte> GetBuffer(int minimumSize)
        {
            int currentOffset = 0;
            int freeSpace = 0;
            while (currentOffset < _bytes.Length)
            {
                var nextBlockSize = BitConverter.ToInt32(_bytes, currentOffset);
                var nextBlockType = _bytes[currentOffset + _headerSize - 1];
                if (nextBlockType == FreeBlock && nextBlockSize >= minimumSize && nextBlockSize < minimumSize + _headerSize) //there are some more bytes than size but cant re-split.
                {
                    ByteUtils.WriteInt(_bytes, currentOffset, nextBlockSize);
                    _bytes[currentOffset + _headerSize] = OccupiedBlock;

                    return new ArraySegment<byte>(_bytes, currentOffset + _headerSize, nextBlockSize);

                }
                if (nextBlockType == FreeBlock && nextBlockSize >= minimumSize + _headerSize) // we can get minimum size and at least write a new header
                {
                    ByteUtils.WriteInt(_bytes, currentOffset, minimumSize);
                    _bytes[currentOffset + _headerSize] = OccupiedBlock;

                    ByteUtils.WriteInt(_bytes, currentOffset + _headerSize + minimumSize, nextBlockSize - minimumSize - _headerSize);
                    _bytes[currentOffset + minimumSize + _headerSize] = FreeBlock;

                    return new ArraySegment<byte>(_bytes, currentOffset + _headerSize, minimumSize);
                }

                currentOffset += _headerSize + nextBlockSize;
            }

            throw new ArgumentOutOfRangeException("Cannot find big enough buffer");

        }

        public void PutBackBuffer(ArraySegment<byte> buffer)
        {
            var bufferOffset = buffer.Offset;
            var nextHeaderType = _bytes[buffer.Offset + buffer.Count + 4];
            if(nextHeaderType == FreeBlock) // merge the free blocks
            {
                var nextBlockSize = BitConverter.ToInt32(_bytes, buffer.Offset + buffer.Count);
                ByteUtils.WriteInt(_bytes, bufferOffset-_headerSize,buffer.Count + _headerSize + nextBlockSize);
                _bytes[bufferOffset + _headerSize - 1] = FreeBlock;
            }
            else
            {
                ByteUtils.WriteInt(_bytes, bufferOffset - _headerSize, buffer.Count);
                _bytes[bufferOffset + _headerSize - 1] = FreeBlock;
            }


        }
    }
}