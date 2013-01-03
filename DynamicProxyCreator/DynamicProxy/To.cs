using System;

namespace com.bodurov.DynamicProxy
{
    public static class To
    {
        private readonly static IProxyCreator Proxy = new ProxyCreator();

        public static Type ProxyType<TSource, TInterface>()
        {
            return Proxy.GetProxyType<TSource, TInterface>();
        }
        public static Type ProxyType(Type sourceType, Type interfaceType)
        {
            return Proxy.GetProxyType(sourceType, interfaceType);
        }
    }
}