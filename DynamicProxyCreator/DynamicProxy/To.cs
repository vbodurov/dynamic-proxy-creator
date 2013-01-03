using System;

namespace com.bodurov.DynamicProxy
{
    public static class To
    {
        public readonly static IProxyCreator DefaultProxyCreator = new ProxyCreator();

        public static Type ProxyType<TSource, TInterface>()
        {
            return DefaultProxyCreator.GetProxyType<TSource, TInterface>();
        }
        public static Type ProxyType(Type sourceType, Type interfaceType)
        {
            return DefaultProxyCreator.GetProxyType(sourceType, interfaceType);
        }
    }
}