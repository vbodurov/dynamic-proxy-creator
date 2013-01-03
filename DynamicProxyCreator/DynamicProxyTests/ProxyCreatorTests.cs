using System;
using NUnit.Framework;
using com.bodurov.DynamicProxy;
using com.bodurov.DynamicProxyTests.TestObjects;

namespace com.bodurov.DynamicProxyTests
{
    [TestFixture]
    public class ProxyCreatorTests
    {

        [Test]
        public void CanProxySimpleMethods()
        {
            var proxy = To.Proxy<IInt01>(new Obj01());

            proxy.Method01();
            proxy.Method02("a", 1, 12.12, true, null, null);
        }

        [Test]
        public void CanProxySimpleMethodsUsingProxyType()
        {
            var type = To.ProxyType<Obj01, IInt01>();

            var a = new Obj01();
            var proxy = (IInt01)Activator.CreateInstance(type, a);

            proxy.Method01();
            proxy.Method02("a", 1, 12.12, true, null, null);
        }

        [Test]
        public void CanProxyClassAndMethodGenerics()
        {
            var proxy = To.Proxy<IInt02<double,string>>(new Obj02<double, string>());


            proxy.Method01("str1", 20);
        }

        [Test]
        public void CanProxyMethodGenerics()
        {
            var proxy = To.Proxy<IInt03>(new Obj03());
            const string testString = "abc";

            var result = proxy.Method03(1, 2, 3.0, true, null, testString);

            Assert.That(result, Is.EqualTo(testString));
        }

        [Test]
        public void CanProxyProperties()
        {
            var proxy = To.Proxy<IInt04>(new Obj04());


            const int testInt = 123;
            const string testString = "abc";

            proxy.IntReadWriteProp = testInt;
            proxy.StringReadWriteProp = testString;

            Assert.That(proxy.IntReadWriteProp, Is.EqualTo(testInt));
            Assert.That(proxy.StringReadWriteProp, Is.EqualTo(testString));
        }

        [Test]
        public void CanProxyPropertiesWithGenerics()
        {
            const double testDouble = 123.8;
            const string testString = "abc";

            var proxy = To.Proxy<IInt05<double,string>>(new Obj05<double, string>());

            proxy.ReadWriteProp = testDouble;
            proxy.ReadWriteProp2 = testString;

            Assert.That(proxy.ReadWriteProp, Is.EqualTo(testDouble));
            Assert.That(proxy.ReadWriteProp2, Is.EqualTo(testString));
        }

        [Test]
        public void CanProxyIndexersWithGenerics()
        {
            var guid = Guid.NewGuid();
            var proxy = To.Proxy<IInt06<Guid>>(new Obj06<Guid>());

            proxy[2.1, Guid.Empty, true, 18.8f, "hey"] = Guid.Empty;


            proxy[guid] = guid.ToString();
            Assert.That(proxy[guid], Is.EqualTo(guid.ToString()));
        }


        [Test]
        public void CanProxyMethodsWithRefAndOut()
        {
            var proxy = To.Proxy<IInt07>(new Obj07());

            var r = 6;
            double half;
            var result = proxy.TryProcess(1, ref r, out half);

            Assert.That(result, Is.True);
            Assert.That(r, Is.EqualTo(7));
            Assert.That(half, Is.EqualTo(r / 2.0));
        }

        [Test]
        public void CanProxyEvents()
        {
            var proxy = To.Proxy<IInt08>(new Obj08());

            var test = 0;

            EventHandler<EventArgs> func = (o, e) => ++test;

            proxy.DoSomething += func;

            proxy.Invoke();
            proxy.Invoke();

            Assert.That(test, Is.EqualTo(2));

            proxy.DoSomething -= func;

            proxy.Invoke();
            proxy.Invoke();
            proxy.Invoke();

            Assert.That(test, Is.EqualTo(2));
        }

        [Test]
        public void CanProxyEventWithGenericArguments()
        {
            var proxy = To.Proxy<IInt09<TestEventArgs>>(new Obj09<TestEventArgs>());

            var test = 0;

            EventHandler<TestEventArgs> func = (o, e) => ++test;

            proxy.DoSomething += func;

            proxy.Invoke(TestEventArgs.Empty);
            proxy.Invoke(TestEventArgs.Empty);

            Assert.That(test, Is.EqualTo(2));

            proxy.DoSomething -= func;

            proxy.Invoke(TestEventArgs.Empty);
            proxy.Invoke(TestEventArgs.Empty);
            proxy.Invoke(TestEventArgs.Empty);

            Assert.That(test, Is.EqualTo(2));

        }

        [Test]
        public void CanProxyEventWithGenericArgumentsUsingProxyType()
        {
            var type = To.ProxyType(typeof(Obj09<>), typeof(IInt09<>));
            type = type.MakeGenericType(typeof(TestEventArgs));

            var obj = new Obj09<TestEventArgs>();
            var proxy = (IInt09<TestEventArgs>)Activator.CreateInstance(type, obj);

            var test = 0;

            EventHandler<TestEventArgs> func = (o, e) => ++test;

            proxy.DoSomething += func;

            proxy.Invoke(TestEventArgs.Empty);
            proxy.Invoke(TestEventArgs.Empty);

            Assert.That(test, Is.EqualTo(2));

            proxy.DoSomething -= func;

            proxy.Invoke(TestEventArgs.Empty);
            proxy.Invoke(TestEventArgs.Empty);
            proxy.Invoke(TestEventArgs.Empty);

            Assert.That(test, Is.EqualTo(2));

        }

        [Test]
        public void CanCacheType()
        {
            var type = To.ProxyType<Obj01, IInt01>();

            Type result;
            var found = To.DefaultProxyCreator.TryFindTypeInCache(typeof (Obj01), typeof (IInt01), out result);

            Assert.That(found);

            var type2 = To.ProxyType<Obj01, IInt01>();

            Assert.That(type , Is.EqualTo(type2));
        }


        [Test]
        public void CanProxyNonGenericType()
        {
            var obj = To.Proxy<IInt01>(new Obj01());

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.ToString(), Is.StringContaining("ProxyForObj01"));
        }

        [Test]
        public void CanProxyGenericType()
        {
            var obj = To.Proxy<IInt05<int, string>>(new Obj05<int, string>());

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.ToString(), Is.StringContaining("ProxyForObj05"));
        }
    }
}