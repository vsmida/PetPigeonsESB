using System;
using System.Collections;
using System.Collections.Generic;

namespace Shared
{
    public class WrappingArrayView<T> : IList<T>
    {
        public readonly T[] Array;
        public int Offset;
        public int OccuppiedLength;

        public WrappingArrayView(int maxSize)
        {
            Array = new T[maxSize];
            OccuppiedLength = 0;
        }

        public WrappingArrayView(T[] array, int offset, int count)
        {
            Array = array;
            OccuppiedLength = count;
            Offset = offset;
        }

        public WrappingArrayView(List<T> failedFrames)
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
            var elementsLeftUntilEndOfArray = Array.Length - Offset;
            if (Count > elementsLeftUntilEndOfArray)
            {
                System.Array.Copy(Array, Offset, array, arrayIndex, elementsLeftUntilEndOfArray);
                System.Array.Copy(Array, 0, array, arrayIndex + elementsLeftUntilEndOfArray, Count - elementsLeftUntilEndOfArray);
            }
            else
            {
                System.Array.Copy(Array, Offset, array, arrayIndex, Count);
            }
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
            get { return Array[(Offset + index) % (Array.Length - 1)]; }
            set { Array[index + Offset] = value; }
        }
    }
}