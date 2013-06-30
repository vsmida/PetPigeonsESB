using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Shared;
using System.Linq;

namespace PgmTransport
{
    internal interface IMessageContainer
    {
        void InsertMessage(ArraySegment<byte> message);
        bool GetNextSegments(out MessageContainerConcurrentQueue.ChunkNode data);
        void PutBackFailedMessage(ArraySegment<byte> unsentMessage);
        int Count { get; }
    }


    internal class MessageContainerConcurrentQueue : IMessageContainer
    {


        public class ChunkNode : IDisposable
        {
            private readonly Pool<ChunkNode> _pool;
            public readonly IList<ArraySegment<byte>> List;
            public int Size;
            public ChunkNode Node;


            public ChunkNode(IList<ArraySegment<byte>> list, Pool<ChunkNode> pool)
            {
                List = list;
                _pool = pool;
            }

            public void Dispose()
            {
                List.Clear();
                Size = 0;
                Node = null;
                _pool.PutBackItem(this);
            }
        }

        private readonly ConcurrentQueue<ArraySegment<byte>> _frames = new ConcurrentQueue<ArraySegment<byte>>();
        private List<ArraySegment<byte>> _failedFrames = new List<ArraySegment<byte>>();
        private readonly int _maxNumberOfElementsPerChunk;
        private readonly int _maxTotalSizePerChunk;
        private readonly Pool<ChunkNode> _chunkPool;
        private ChunkNode _currentWritingChunk;
        private ChunkNode _currentReadingChunk;
        //private volatile int _chunkWrittenCount = 0; // one writer for now
        //private volatile int _chunkReadCount; //one reader
        //private int _canSteal = 1;  //0 cant , 1 can
        //private volatile bool _isEmpty = true;
        private SpinLock _spinLock = new SpinLock();

        public MessageContainerConcurrentQueue(int maxNumberOfElementsPerChunk, int maxTotalSizePerChunk)
        {
            _maxNumberOfElementsPerChunk = 2 * maxNumberOfElementsPerChunk;
            _maxTotalSizePerChunk = maxTotalSizePerChunk;
            _chunkPool = new Pool<ChunkNode>(() => new ChunkNode(new List<ArraySegment<byte>>(_maxNumberOfElementsPerChunk), _chunkPool), 0);
            _chunkPool.AddCapacity(100);
            _currentReadingChunk = _chunkPool.GetItem();
            _currentWritingChunk = _currentReadingChunk;
        }

        public void InsertMessage(ArraySegment<byte> message)
        {
            bool lockTaken = false;
            var size = new ArraySegment<byte>(BitConverter.GetBytes(message.Count));
            _spinLock.Enter(ref lockTaken);
            {
                _currentWritingChunk.List.Add(size);
                _currentWritingChunk.List.Add(message);
                _currentWritingChunk.Size += message.Count + 4;
                if (_currentWritingChunk.List.Count == _maxNumberOfElementsPerChunk || _currentWritingChunk.Size >= _maxTotalSizePerChunk) //need new chunk anyway
                {
                    _currentWritingChunk.Node = _chunkPool.GetItem();
                    _currentWritingChunk = _currentWritingChunk.Node;
                }
            }
            _spinLock.Exit(false);

        }

        public bool GetNextSegments(out ChunkNode data)
        {
            if (_failedFrames.Count > 0) //single threaded access to failed frames
            {
                data = new ChunkNode(_failedFrames, _chunkPool) { Size = _failedFrames.Sum(x => x.Count) };
                _failedFrames = new List<ArraySegment<byte>>();
                return true;
            }

            bool lockTaken = false;
            _spinLock.Enter(ref lockTaken);
            {

                if (_currentReadingChunk != _currentWritingChunk)
                {
                    data = _currentReadingChunk;
                    _currentReadingChunk = _currentReadingChunk.Node;
                    _spinLock.Exit(false);
                    return true;
                }
                else
                {
                    if (_currentReadingChunk.Size == 0)
                    {
                        _spinLock.Exit(false);
                        data = null;
                        return false;
                    }
                    else
                    {
                        data = _currentReadingChunk;
                        _currentReadingChunk.Node = _chunkPool.GetItem();
                        _currentReadingChunk = _currentReadingChunk.Node;
                        _currentWritingChunk = _currentReadingChunk;
                        _spinLock.Exit(false);
                        return true;
                    }
                }

            }



        }


        public void PutBackFailedMessage(ArraySegment<byte> unsentMessage) //single threaded access
        {
            _failedFrames.Add(unsentMessage);
        }

        public int Count
        {
            get { return _failedFrames.Count + _frames.Count; }
        }
    }
}