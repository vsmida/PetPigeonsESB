using System;
using System.Collections.Concurrent;

namespace Shared
{
    public class Pool<T>
    {
        private readonly ConcurrentStack<T> _pool = new ConcurrentStack<T>();
        private readonly Func<T> _itemFactory;

        public Pool(Func<T> itemFactory, int initialCapacity = 0)
        {
            _itemFactory = itemFactory;
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_itemFactory());
            }
        }

        public T GetItem()
        {
            T item;
            if(!_pool.TryPop(out item))
                return _itemFactory();
            return item;
        }

        public void PutBackItem(T item)
        {
            _pool.Push(item);
        }
    }
}