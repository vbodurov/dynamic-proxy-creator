using System;
using DynamicProxy;
using DynamicProxyTests.TestObjects;
using NUnit.Framework;

namespace DynamicProxyTests
{
    [TestFixture]
    public class ProxyCreatorTests
    {
        [Test]
        public void CanProxySimpleMethods()
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
            var type = To.ProxyType(typeof(Obj02<,>), typeof(IInt02<,>));
            type = type.MakeGenericType(new[] { typeof(double), typeof(string) });

            var a = new Obj02<double, string>();
            var proxy = (IInt02<double, string>)Activator.CreateInstance(type, a);


            proxy.Method01("str1", 20);
        }

        [Test]
        public void CanProxyMethodGenerics()
        {
            var type = To.ProxyType<Obj03, IInt03>();

            var a = new Obj03();
            var proxy = (IInt03)Activator.CreateInstance(type, a);
            const string testString = "abc";

            var result = proxy.Method03(1, 2, 3.0, true, null, testString);

            Assert.That(result, Is.EqualTo(testString));
        }

        [Test]
        public void CanProxyProperties()
        {
            var type = To.ProxyType<Obj04, IInt04>();

            var a = new Obj04();
            var proxy = (IInt04)Activator.CreateInstance(type, a);
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
            var type = To.ProxyType(typeof(Obj05<,>), typeof(IInt05<,>));
            type = type.MakeGenericType(new[] { typeof(double), typeof(string) });
            const double testDouble = 123.8;
            const string testString = "abc";

            var a = new Obj05<double, string>();
            var proxy = (IInt05<double, string>)Activator.CreateInstance(type, a);

            proxy.ReadWriteProp = testDouble;
            proxy.ReadWriteProp2 = testString;

            Assert.That(proxy.ReadWriteProp, Is.EqualTo(testDouble));
            Assert.That(proxy.ReadWriteProp2, Is.EqualTo(testString));
        }

        [Test]
        public void CanProxyIndexersWithGenerics()
        {
            var guid = Guid.NewGuid();
            var type = To.ProxyType(typeof(Obj06<>), typeof(IInt06<>));
            type = type.MakeGenericType(new[] { typeof(Guid) });

            var a = new Obj06<Guid>();
            var proxy = (IInt06<Guid>)Activator.CreateInstance(type, a);

            proxy[2.1, Guid.Empty, true, 18.8f, "hey"] = Guid.Empty;


            proxy[guid] = guid.ToString();
            Assert.That(proxy[guid], Is.EqualTo(guid.ToString()));
        }
    }

}