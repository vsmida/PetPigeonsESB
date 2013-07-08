using System;
using System.Collections.Generic;

namespace Shared
{
    public class StackPool<T> : IPool<T>
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Func<T> _itemFactory;

        public StackPool(Func<T> itemFactory, int initialCapacity = 0)
        {
            _itemFactory = itemFactory;
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_itemFactory());
            }
        }

        public T GetItem()
        {
            if (_pool.Count == 0)
                return _itemFactory();
            return _pool.Pop();
        }

        public void PutBackItem(T item)
        {
            _pool.Push(item);
        }
    }
}