using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Shared;
using System.Linq;

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

        public MessageContainerConcurrentQueue(int maxNumberOfElementsPerChunk, int maxTotalSizePerChunk)
        {
            _maxNumberOfElementsPerChunk = maxNumberOfElementsPerChunk;
            _maxNumberOfListElements = 2 * maxNumberOfElementsPerChunk;


            _maxTotalSizePerChunk = maxTotalSizePerChunk;
            _backingArray = new ArraySegment<byte>[_maxNumberOfListElements];
            for (int i = 0; i < maxNumberOfElementsPerChunk; i++)
            {
                _backingArray[2*i] = new ArraySegment<byte>(new byte[4],0,4);
            }

            _returnList = new WrappingArrayView<ArraySegment<byte>>(_backingArray, 0, 0);
        }


        public void InsertMessage(ArraySegment<byte> message)
        {
            var previousSequence = (Interlocked.Add(ref _nextWritingSequence, 2) - 2);


            //claiming strat? //dont wrap
            var volatileRead = Thread.VolatileRead(ref _currentReadingSequence);
            while ( previousSequence - volatileRead >= _maxNumberOfListElements)
            {
                Thread.Sleep(0);
              //  _spinWait.SpinOnce();
                volatileRead = Thread.VolatileRead(ref _currentReadingSequence);
            }


            var indexToWrite = previousSequence & (_maxNumberOfListElements-1); //get next writable sequence
         //   var size = BitConverter.GetBytes(message.Count);
            //write
            ByteUtils.WriteInt(_backingArray[indexToWrite].Array, 0, message.Count);
            //_backingArray[indexToWrite].Array = size;
            //_backingArray[indexToWrite].Count = 4;
            //_backingArray[indexToWrite].Offset = 0;

            _backingArray[indexToWrite + 1] = message;
            //_backingArray[indexToWrite + 1].Array = message.Array;
            //_backingArray[indexToWrite + 1].Count = message.Count;
            //_backingArray[indexToWrite + 1].Offset = message.Offset;
            ////end write

            //commit phase
            while (Interlocked.CompareExchange(ref _maxReadableSequence, previousSequence + 2, previousSequence) != (previousSequence))//commit after all other writers before me have commited
            {
                _spinWait.SpinOnce();
            }

        }

        public bool GetNextSegments(out IList<ArraySegment<byte>> data)
        {
            //only one reader.
            var maxReadableSequence = Thread.VolatileRead(ref _maxReadableSequence); //this is shared state, try to get last value

            if (maxReadableSequence <= _currentReadingSequence) //can only equal though
            {
                data = null;
                return false;
            }

            _returnList.Offset = (int)(_currentReadingSequence & (_maxNumberOfListElements - 1)); //_currentReading not modified from other thread
            _returnList.OccuppiedLength = (int)(maxReadableSequence - _currentReadingSequence);

            data = _returnList;
            return true;

        }

        public void FlushMessages(IList<ArraySegment<byte>> data)
        {
            Thread.VolatileWrite(ref _currentReadingSequence, _currentReadingSequence + data.Count); //to show other threads
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