using System.Collections.Generic;

namespace Shared
{
    public static class ExtendDictionary
    {
        public static U GetOrCreateNew<T, U>(this Dictionary<T, U> dictionary, T key) where U : new()
        {
            U item;
            if (!dictionary.TryGetValue(key, out item))
            {
                item = new U();
                dictionary[key] = item;
            }
            return item;
        }

        public static U GetValueOrDefault<T, U>(this Dictionary<T, U> dictionary, T key, U defaultValue = default(U))
        {
            U item;
            if (!dictionary.TryGetValue(key, out item))
                return defaultValue;
            return item;

        }


    }
}