using System;

namespace com.bodurov.DynamicProxy
{
    public interface IProxyCreator
    {
        Type GetProxyType<TSource, TInterface>();
        Type GetProxyType(Type sourceType, Type interfaceType);
        bool TryFindTypeInCache(Type sourceType, Type interfaceType, out Type cachedType);
    }
}