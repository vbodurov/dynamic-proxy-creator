namespace DynamicProxyTests.TestObjects
{
    public interface IInt06<T>
    {
        T this[int i] { get; }
        T this[double i, T t, bool b, float f, string s] { get; set; }
        string this[T t] { get; set; }
    }


    public class Obj06<T>
    {
        private T _t;
        public T this[int i] {
            get { return _t; }
        }
        public T this[double i, T t, bool b, float f, string s]
        {
            set {  }
            get { return _t; }
        }
        public string this[T t]
        {
            get { return _t.ToString(); }
            set { _t = t; }
        }
    }

    public sealed class Obj06Proxy<T> : IInt06<T>
    {
        private readonly Obj06<T> _source;

        public Obj06Proxy(Obj06<T> source)
        {
            _source = source;
        }


        T IInt06<T>.this[int i]
        {
            get { return _source[i]; }
        }
        T IInt06<T>.this[double i, T t, bool b, float f, string s]
        {
            get { return _source[i, t, b, f, s]; }
            set { _source[i, t, b, f, s] = value; }
        }
        string IInt06<T>.this[T t]
        {
            get { return _source[t]; }
            set { _source[t] = value; }
        }
    }
    
}