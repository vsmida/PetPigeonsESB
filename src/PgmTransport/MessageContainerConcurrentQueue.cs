using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Shared;

namespace PgmTransport
{
    internal interface IMessageContainer
    {
        void InsertMessage(ArraySegment<byte> message);
        bool TryGetNextSegments(out IList<ArraySegment<byte>> data);
        int Count { get; }
        void FlushMessages(IList<ArraySegment<byte>> data);
    }


    internal interface ICompletionWorkQueue
    {
    }

    internal class CompletionWorkQueue : ICompletionWorkQueue
    {
        private readonly int _maxNumberOfListElements;
        private readonly Stream[] _backingArray;
        private long _currentReadingSequence;
        private long _nextWritingSequence;
        private long _maxReadableSequence;
        private readonly int _indexMask;

        public CompletionWorkQueue(int maxNumberOfListElements)
        {
            _maxNumberOfListElements = maxNumberOfListElements;
            _indexMask = maxNumberOfListElements - 1;
            _backingArray = new Stream[_maxNumberOfListElements];
        }

        public void InsertStream(Stream stream)
        {
            var sequenceToClaim = _nextWritingSequence;

            while(_nextWritingSequence - _currentReadingSequence >= _maxNumberOfListElements)
            {
                Thread.Sleep(0);
                Thread.MemoryBarrier();
            }

            _backingArray[sequenceToClaim & _indexMask] = stream;
            _nextWritingSequence++;
            _maxReadableSequence++;
            Thread.MemoryBarrier();
        }

        public bool TryGetNextStream(out Stream stream)
        {
            //only one reader.
            // last memory barrier in insert message insures some freshness
            if (_maxReadableSequence <= _currentReadingSequence) //can only equal though
            {
                stream = null;
                return false;
            }

            stream = _backingArray[_currentReadingSequence];
            _currentReadingSequence++;
            Thread.MemoryBarrier();
            return true;
        }

    }
    internal class MessageContainerConcurrentQueue : IMessageContainer
    {

        private readonly int _maxNumberOfListElements;
        private readonly int _maxTotalSizePerChunk;
        private readonly ArraySegment<byte>[] _backingArray;
        private long _currentReadingSequence;
        private long _nextWritingSequence;
        private long _maxReadableSequence;
        private readonly WrappingArrayView<ArraySegment<byte>> _returnList;
        private readonly long _maxNumberOfElementsPerChunk;
        private readonly int _indexMask;
        private readonly byte[] _arrayForSizes;

        public MessageContainerConcurrentQueue(int maxNumberOfElementsPerChunk, int maxTotalSizePerChunk)
        {
            _maxNumberOfElementsPerChunk = maxNumberOfElementsPerChunk;
            _maxNumberOfListElements = 2 * maxNumberOfElementsPerChunk;
            _maxTotalSizePerChunk = maxTotalSizePerChunk;
            _backingArray = new ArraySegment<byte>[_maxNumberOfListElements];
            _returnList = new WrappingArrayView<ArraySegment<byte>>(_backingArray, 0, 0);
            _indexMask = 2 * maxNumberOfElementsPerChunk - 1;

            _arrayForSizes = new byte[maxNumberOfElementsPerChunk * 4];
            for (int i = 0; i < maxNumberOfElementsPerChunk; i++)
            {
                _backingArray[2*i] = new ArraySegment<byte>(_arrayForSizes,i*4,4);
            }
        }

        public void InsertMessage(ArraySegment<byte> message)
        {
            //claim sequence
             var previousSequence = (Interlocked.Add(ref _nextWritingSequence, 2) - 2);
            //memory barrier here due to interlocked
            
            
            //dont wrap
            while (previousSequence - _currentReadingSequence >= 2 * _maxNumberOfElementsPerChunk)
            {
                Thread.Sleep(0);
                Thread.MemoryBarrier(); //ensure value is read again
            }

            //write
            var indexToWrite = previousSequence & (_indexMask);
            ByteUtils.WriteInt(_arrayForSizes, (int)(indexToWrite/ 2 * 4), message.Count);
            _backingArray[indexToWrite + 1] = message;
            ////end write

            //commit phase
            while (_maxReadableSequence != previousSequence)
                // will update due to last memory barrier
            {
                default(SpinWait).SpinOnce();
                Thread.MemoryBarrier();
            }
            _maxReadableSequence += 2;
            Thread.MemoryBarrier();

        }

        public bool TryGetNextSegments(out IList<ArraySegment<byte>> data)
        {
            //only one reader.
            // last memory barrier in insert message insures some freshness
            if (_maxReadableSequence <= _currentReadingSequence) //can only equal though
            {
                data = null;
                return false;
            }

            _returnList.Offset = (int)(_currentReadingSequence & (_indexMask)); //_currentReading not modified from other thread
            _returnList.OccuppiedLength = (int)(_maxReadableSequence - _currentReadingSequence); //get the field, who knows we can get a few more items
            data = _returnList;
            return true;

        }

        public void FlushMessages(IList<ArraySegment<byte>> data)
        {
            _currentReadingSequence += data.Count;
            Thread.MemoryBarrier(); //"publish" change
        }

        public int Count
        {
            get
            {
                return (int)(_nextWritingSequence - _currentReadingSequence);
            }
        }
    }
}