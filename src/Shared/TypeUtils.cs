using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Shared
{
    public static class TypeUtils
    {
        [ThreadStatic]
        private static readonly Dictionary<string, Type> _cache = new Dictionary<string, Type>(); 

        public static Type Resolve(string fullName)
        {
            Debug.Assert(fullName != null);
            Type cachedType;
            if (_cache.TryGetValue(fullName, out cachedType))
                return cachedType;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    _cache[fullName] = type;
                    return type;                    
                }
            }
            throw new ArgumentException("Type " + fullName + " doesn't exist in the current app domain");
        }
    }
}