using System;
using System.Collections.Generic;
using System.Threading;
using Shared;

namespace PgmTransport
{
    internal interface IMessageContainer
    {
        void InsertMessage(ArraySegment<byte> message);
        bool GetNextSegments(out IList<ArraySegment<byte>> data);
        //  void PutBackFailedMessage(ArraySegment<byte> unsentMessage);
        int Count { get; }
        void FlushMessages(IList<ArraySegment<byte>> data);
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
        private long _maxNumberOfElementsPerChunk;
        private readonly SpinWait _spinWait = new SpinWait();
        private int _indexMask;

        public MessageContainerConcurrentQueue(int maxNumberOfElementsPerChunk, int maxTotalSizePerChunk)
        {
            _maxNumberOfElementsPerChunk = maxNumberOfElementsPerChunk;
            _maxNumberOfListElements = 2 * maxNumberOfElementsPerChunk;
            _maxTotalSizePerChunk = maxTotalSizePerChunk;
            _backingArray = new ArraySegment<byte>[_maxNumberOfListElements];
            _returnList = new WrappingArrayView<ArraySegment<byte>>(_backingArray, 0, 0);
            // _indexMask = _maxNumberOfListElements-1;
            _indexMask = 2 * maxNumberOfElementsPerChunk - 1;

            for (int i = 0; i < maxNumberOfElementsPerChunk; i++)
            {
                _backingArray[2*i] = new ArraySegment<byte>(new byte[4],0,4);
            }
        }


        public void InsertMessage(ArraySegment<byte> message)
        {
            //claim sequence
             var previousSequence = (Interlocked.Add(ref _nextWritingSequence, 2) - 2);

            //dont wrap
            while (previousSequence - Interlocked.Read(ref _currentReadingSequence) >= 2 * _maxNumberOfElementsPerChunk)
            {
                Thread.Sleep(0);
              //    _spinWait.SpinOnce();
            }

            //write
            var indexToWrite = previousSequence & (_indexMask);
            ByteUtils.WriteInt(_backingArray[indexToWrite].Array,0,message.Count);
            _backingArray[indexToWrite + 1] = message;
            ////end write

            //commit phase
            while (Interlocked.CompareExchange(ref _maxReadableSequence, previousSequence + 2, previousSequence) != (previousSequence))//commit after all other writers before me have commited
            {
                default(SpinWait).SpinOnce();
            }

        }

        public bool GetNextSegments(out IList<ArraySegment<byte>> data)
        {
            //only one reader.
            var maxReadableSequence = Interlocked.Read(ref _maxReadableSequence);

            if (maxReadableSequence <= _currentReadingSequence) //can only equal though
            {
                data = null;
                return false;
            }

            _returnList.Offset = (int)(_currentReadingSequence & (_indexMask)); //_currentReading not modified from other thread
            _returnList.OccuppiedLength = (int)(maxReadableSequence - _currentReadingSequence);
            data = _returnList;
            return true;

        }

        public void FlushMessages(IList<ArraySegment<byte>> data)
        {
            _currentReadingSequence += data.Count;
        }


        //public void PutBackFailedMessage(ArraySegment<byte> unsentMessage) //single threaded access
        //{
        //    _failedFrames.Add(unsentMessage);
        //}

        public int Count
        {
            get { return (int)(_nextWritingSequence - _currentReadingSequence); }
        }
    }
}