using System;

namespace com.bodurov.DynamicProxyTests.TestObjects
{
    public interface IInt01
    {
        void Method01();
        string Method02(string s, int i, double d, bool b, Obj01 a, object o);

    }

    public class Obj01
    {
        public void Method01()
        {
            Console.WriteLine("Obj01.Method01");
        }
        public string Method02(string s, int i, double d, bool b, Obj01 a, object o)
        {
            Console.WriteLine(String.Join(" ", new[] { s, i, d, b, a, o }));
            return "Method02";
        }
    }
    // used to find out how it is wired on IL level
    public class Obj01Proxy : IInt01
    {
        private readonly Obj01 _a;

        public Obj01Proxy(Obj01 a)
        {
            _a = a;
        }

        void IInt01.Method01()
        {
            _a.Method01();
        }
        string IInt01.Method02(string s, int i, double d, bool b, Obj01 a, object o)
        {
            return _a.Method02(s, i, d, b, a, o);
        }
    }
}