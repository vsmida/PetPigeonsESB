using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Shared
{
    public static class TypeUtils
    {
        public static Type Resolve(string fullName)
        {
            Debug.Assert(fullName != null);

            List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

            foreach (var assembly in assemblies)
            {
                Type t = assembly.GetType(fullName, false);
                if (t != null)
                    return t;
            }
            throw new ArgumentException("Type " + fullName + " doesn't exist in the current app domain");
        }
    }
}