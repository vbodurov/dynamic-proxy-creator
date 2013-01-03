using System;

namespace com.bodurov.DynamicProxyTests.TestObjects
{
    public interface IInt03
    {
        T2 Method03<T1, T2>(T1 s, int i, double d, bool b, Obj03 a, T2 o) where T1 : struct;

    } 

    public class Obj03
    {
        public T2 Method03<T1, T2>(T1 s, int i, double d, bool b, Obj03 a, T2 o) where T1 : struct
        {
            Console.WriteLine(String.Join(" ", new object[] { s, i, d, b, a, o }));
            return o;
        }
    }
}