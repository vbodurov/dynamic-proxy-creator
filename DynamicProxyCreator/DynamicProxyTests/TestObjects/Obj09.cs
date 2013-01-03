using System;
using System.Threading;

namespace com.bodurov.DynamicProxyTests.TestObjects
{
    public interface IInt09<T> where T :EventArgs
    {
        event EventHandler<T> DoSomething;
        void Invoke(T t);
    }

    public class TestEventArgs : EventArgs
    {
        public new static readonly TestEventArgs Empty = new TestEventArgs();
    }

    public class Obj09<T> where T : EventArgs
    {
        public event EventHandler<T> DoSomething;
        public void Invoke(T t)
        {
            if (DoSomething != null)
            {
                DoSomething(this, t);
            }
        }
    }
}