using System;
using System.Reflection;

namespace Shared
{
    public class ProxyDomain : MarshalByRefObject
    {
        public Assembly GetAssembly(string AssemblyPath)
        {
            try
            {
                return Assembly.LoadFrom(AssemblyPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}