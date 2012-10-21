using System;

namespace ZmqServiceBus.Bus
{
    public interface IObjectFactory
    {
        T GetInstance<T>();
        object GetInstance(Type type);
    }

    public class ObjectFactory : IObjectFactory
    {
        public T GetInstance<T>()
        {
            return StructureMap.ObjectFactory.GetInstance<T>();
        }

        public object GetInstance(Type type)
        {
            return StructureMap.ObjectFactory.GetInstance(type);
        }
    }
}