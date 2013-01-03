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
        public static TInterface Proxy<TInterface>(object sourceToWrap)
        {
            if (sourceToWrap == null) return default(TInterface);
            var sourceType = sourceToWrap.GetType();
            var type = DefaultProxyCreator.GetProxyType(sourceType, typeof(TInterface));
            if (type.ContainsGenericParameters)
            {
                type = type.MakeGenericType(sourceType.GetGenericArguments());
            }
            return (TInterface) Activator.CreateInstance(type, sourceToWrap);
        }
    }
}