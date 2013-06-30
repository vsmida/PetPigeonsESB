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

        private struct MutableArraySegment
        {
            public byte[] Array;
            public int Offset;
            public int Count;

            public int Count1;
            public int Count2;
            public int Count3;
            public int Count4;
            public int Count5;
            public int Count6;
            public int Count7;
            public int Count8;
            public int Count9;
            public int Count10;
            public int Count11;
            public int Count12; //60
            public int Count13; //64

            //    public MutableArraySegment(){}

            public MutableArraySegment(byte[] array, int offset, int count)
            {
                Array = array;
                Offset = offset;
                Count = count;

                Count1 = 1;
                Count2 = 1;
                Count3 = 1;
                Count4 = 1;
                Count5 = 1;
                Count6 = 1;
                Count7 = 1;
                Count8 = 1;
                Count9 = 1;
                Count10 = 1;
                Count11 = 1;
                Count12 = 1;
                Count13 = 1;

            }
        }

        private class MutableSegmentList : IList<ArraySegment<byte>>
        {
            public long Offset;
            public long UnderlyingCount;
            public MutableArraySegment[] Array;


            public IEnumerator<ArraySegment<byte>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(ArraySegment<byte> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                UnderlyingCount = 0;
            }

            public bool Contains(ArraySegment<byte> item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(ArraySegment<byte>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(ArraySegment<byte> item)
            {
                throw new NotImplementedException();
            }

            public int Count { get { return (int)UnderlyingCount; } }
            public bool IsReadOnly { get; private set; }
            public int IndexOf(ArraySegment<byte> item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, ArraySegment<byte> item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public ArraySegment<byte> this[int index]
            {
                get
                {
                    var mutableArraySegment = Array[(Offset + index) & (Array.Length - 1)];
                    return new ArraySegment<byte>(mutableArraySegment.Array, mutableArraySegment.Offset, mutableArraySegment.Count);
                }
                set { throw new NotImplementedException(); }
            }
        }

        private readonly int _maxNumberOfListElements;
        private readonly int _maxTotalSizePerChunk;
        private readonly MutableArraySegment[] _backingArray;
        private long _currentReadingSequence = 0;
        private long _nextWritingSequence;
        private long _maxReadableSequence = 0;
        private readonly MutableSegmentList _returnList = new MutableSegmentList();
        private long _maxNumberOfElementsPerChunk;
        private readonly SpinWait _spinWait = new SpinWait();

        public MessageContainerConcurrentQueue(int maxNumberOfElementsPerChunk, int maxTotalSizePerChunk)
        {
            _maxNumberOfElementsPerChunk = maxNumberOfElementsPerChunk;
            _maxNumberOfListElements = 2 * maxNumberOfElementsPerChunk;
            _maxTotalSizePerChunk = maxTotalSizePerChunk;
            _backingArray = new MutableArraySegment[_maxNumberOfListElements];
            //for (int i = 0; i < _backingArray.Count(); i++)
            //{
            //    _backingArray[i] = new MutableArraySegment();
            //}
            _returnList.Array = _backingArray;
        }


        public void InsertMessage(ArraySegment<byte> message)
        {
            var previousSequence = (Interlocked.Add(ref _nextWritingSequence, 2) - 2);


            //claiming strat? //dont wrap
            var volatileRead = Thread.VolatileRead(ref _currentReadingSequence);
           // while ( (volatileRead & (_maxNumberOfListElements - 1)) <= ((previousSequence & (_maxNumberOfListElements - 1)))    && (previousSequence / _maxNumberOfListElements > volatileRead / _maxNumberOfListElements)) //wrong condition
            while ( previousSequence - volatileRead >= _maxNumberOfListElements) //wrong condition
            {
                _spinWait.SpinOnce();
                volatileRead = Thread.VolatileRead(ref _currentReadingSequence);
            }


            var indexToWrite = previousSequence & (_maxNumberOfListElements-1); //get next writable sequence
            var size = BitConverter.GetBytes(message.Count);
            //write
            _backingArray[indexToWrite].Array = size;
            _backingArray[indexToWrite].Count = 4;
            _backingArray[indexToWrite].Offset = 0;

            _backingArray[indexToWrite + 1].Array = message.Array;
            _backingArray[indexToWrite + 1].Count = message.Count;
            _backingArray[indexToWrite + 1].Offset = message.Offset;
            //end write

            //commit phase
            while (Interlocked.CompareExchange(ref _maxReadableSequence, previousSequence + 2, previousSequence) != (previousSequence))//commit after all other writers before me have commited
            {
                default(SpinWait).SpinOnce();
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


            //if (_maxNumberOfElementsPerChunk < (maxReadableSequence - _currentReadingSequence))
            //    _returnList.UnderlyingCount = _maxNumberOfElementsPerChunk;
            //else
            //{
            _returnList.UnderlyingCount = (maxReadableSequence - _currentReadingSequence);
            //    }

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