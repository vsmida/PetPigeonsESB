using System;
using System.Collections;
using System.Collections.Generic;

namespace Shared
{
    public class PreAllocatedArray<T> : IList<T>
    {
        public readonly T[] Array;
        public int OccuppiedLength;
        private int _maxSize;

        public PreAllocatedArray(int maxSize)
        {
            _maxSize = maxSize;
            Array = new T[maxSize];
            OccuppiedLength = 0;
        }

        public PreAllocatedArray(List<T> failedFrames)
        {
            Array = failedFrames.ToArray();
            OccuppiedLength = failedFrames.Count;
        }

        public void Add(T item)
        {
            Array[OccuppiedLength] = item;
            OccuppiedLength++;
        }

        public void Clear()
        {
            OccuppiedLength = 0;
        }

        public bool Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        public int Count { get { return OccuppiedLength; } }
        public bool IsReadOnly { get { return false; } }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        public T this[int index]
        {
            get { return Array[index]; }
            set { Array[index] = value; }
        }
    }
}