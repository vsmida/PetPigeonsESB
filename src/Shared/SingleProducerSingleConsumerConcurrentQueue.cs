using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Shared
{

    public class SingleProducerSingleConsumerConcurrentQueue<T> : IProducerConsumerCollection<T>
    {

        private class BufferPool
        {
            private readonly ConcurrentBag<Buffer> _buffers = new ConcurrentBag<Buffer>();

            public  Buffer GetBuffer()
            {
                Buffer buff;
                    if(_buffers.TryTake(out buff))
                        return buff;
                return new Buffer();
            }

            public  void PutBackBuffer(Buffer buffer)
            {
                if(buffer == null)
                    throw new Exception("cannot put null buffer");
                _buffers.Add(buffer);
            }

        }

        private class Buffer
        {
            public static int BufferSize;
            public T[] Elements = new T[BufferSize];
            public Buffer NextBuffer;

        }

        private  Buffer _writeBuffer;
        private  Buffer _readBuffer;
        private readonly BufferPool _pool = new BufferPool();
        private int _readBufferIndex = 0;
        private int _writeBufferIndex = 0;

        public SingleProducerSingleConsumerConcurrentQueue(int size)
        {
            Buffer.BufferSize = size;
            _writeBuffer = new Buffer();
            _readBuffer = _writeBuffer;

        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count { get; private set; }
        public object SyncRoot { get; private set; }
        public bool IsSynchronized { get; private set; }
        public void CopyTo(T[] array, int index)
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(T item)
        {
            _writeBuffer.Elements[_writeBufferIndex] = item;
            _writeBufferIndex++;
            if (_writeBufferIndex == Buffer.BufferSize)
            {
                _writeBuffer.NextBuffer = _pool.GetBuffer();
                _writeBufferIndex = 0;
                _writeBuffer = _writeBuffer.NextBuffer;
            }
            return true;
        }

        public bool TryTake(out T item)
        {
            //empty case
            if (_readBuffer != _writeBuffer) //if we have old value of writebuffer, we are fucked i guess, volatile should take care of it?nope
            {
                if (_readBufferIndex == Buffer.BufferSize) //have to move to the next buffer, which is at worst the writebuffer if the previous condition were true but maybe not
                {
                    if (_readBuffer.NextBuffer != null) //someone has writen, it s good we are the only consumer
                    {
                        var nextBuffer = _readBuffer.NextBuffer;
                        _pool.PutBackBuffer(_readBuffer);
                        _readBuffer = nextBuffer;
                        _readBufferIndex = 0;
                        return TryTake(out item);
                    }
                    else
                    {
                        item = default(T);
                        return false;
                    }

                }

                item = _readBuffer.Elements[_readBufferIndex];
                // _readBuffer.Elements[_readBufferIndex] = default(T);
                _readBufferIndex++;



                return true;
            }
            if (_readBufferIndex < _writeBufferIndex && _readBufferIndex < Buffer.BufferSize) //can always pop if only one consumer
            {
                item = _readBuffer.Elements[_readBufferIndex];
                // _readBuffer.Elements[_readBufferIndex] = default(T);
                _readBufferIndex++;

                return true;
            }

            if (_readBufferIndex == Buffer.BufferSize) //we are at end of buffer, but there may not be a Next one
            {
                if (_readBuffer.NextBuffer != null) //someone has writen, it s good we are the only consumer
                {
                    var nextBuffer = _readBuffer.NextBuffer;
                    _pool.PutBackBuffer(_readBuffer);
                    _readBuffer = nextBuffer;
                    _readBufferIndex = 0;
                }
                item = default(T);
                return false;
            }

            if (_readBufferIndex == _writeBufferIndex)//overtakingwriter
            {
                item = default(T);
                return false;
            }

            return TryTake(out item);
        }

        public T[] ToArray()
        {
            throw new NotImplementedException();
        }
    }
}