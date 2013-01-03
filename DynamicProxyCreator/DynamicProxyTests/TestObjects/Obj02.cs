using System;

namespace DynamicProxyTests.TestObjects
{
    public interface IInt02<T1, T2> where T2 : class
    {
        T1 Method01<T3>(T2 arg, T3 x);
    }

    public class Obj02<T1, T2>
    {
        public T1 Method01<T3>(T2 arg, T3 x)
        {
            Console.WriteLine("Method01 " + arg + " " + x);
            return default(T1);
        }
    }

    public class Obj02Proxy<T1, T2> : IInt02<T1, T2> where T2 : class
    {
        private readonly Obj02<T1, T2> _source;

        public Obj02Proxy(Obj02<T1, T2> s)
        {
            _source = s;
        }


        T1 IInt02<T1, T2>.Method01<T3>(T2 arg, T3 x)
        {
            return _source.Method01(arg, x);
        }
    }
}