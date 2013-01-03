using System;
using System.Threading;

namespace com.bodurov.DynamicProxyTests.TestObjects
{
    public interface IInt08
    {
        event EventHandler<EventArgs> DoSomething;
        void Invoke();
    }


    public class Obj08
    {
        public event EventHandler<EventArgs> DoSomething;
        public void Invoke()
        {
            if (DoSomething != null)
            {
                DoSomething(this, EventArgs.Empty);
            }
        }
    }

    // used to find out how it is wired on IL level
    public class Obj08Proxy: IInt08
    {
        private readonly Obj08 _source;

        public Obj08Proxy(Obj08 source)
        {
            _source = source;
        }


        event EventHandler<EventArgs> IInt08.DoSomething
        {
            add { _source.DoSomething += value; }
            remove { _source.DoSomething -= value; }

        }

        void IInt08.Invoke()
        {
            _source.Invoke();
        } 
    }
}