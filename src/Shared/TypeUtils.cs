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
        private static Dictionary<string, Type> _cache;


        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck, out Type actualGeneric)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    actualGeneric = toCheck;
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            actualGeneric = null;
            return false;
        }

        public static Type Resolve(string fullName)
        {
            if(_cache == null)
                _cache = new Dictionary<string, Type>();
            Debug.Assert(fullName != null);
            Debug.Assert(_cache != null);
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