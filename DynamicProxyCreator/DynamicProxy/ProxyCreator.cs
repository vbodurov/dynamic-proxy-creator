using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace com.bodurov.DynamicProxy
{
    public class ProxyCreator : IProxyCreator
    {
        private readonly IProxyCreator _interface;
        private readonly IDictionary<Type,IDictionary<Type,Type>> _cacheByInterfaceType = new Dictionary<Type, IDictionary<Type, Type>>(); 
        private static int _counter = 100;

        public ProxyCreator()
        {
            _interface = this;
        }

        bool IProxyCreator.TryFindTypeInCache(Type sourceType, Type interfaceType, out Type cachedType)
        {
            IDictionary<Type, Type> cacheBySourceType;
            if (_cacheByInterfaceType.TryGetValue(interfaceType, out cacheBySourceType))
            {
                if (cacheBySourceType.TryGetValue(sourceType, out cachedType))
                {
                    return true;
                }
            }
            else
            {
                cachedType = null;
            }
            return false;
        }

        Type IProxyCreator.GetProxyType<TSource, TInterface>()
        {
            return _interface.GetProxyType(typeof(TSource), typeof(TInterface));
        }

        Type IProxyCreator.GetProxyType(Type sourceType, Type interfaceType)
        {
            if (interfaceType.IsGenericType)
            {
                if (!sourceType.IsGenericType)
                {
                    throw new InvalidOperationException(
                        String.Format("Interface type '{0}' is generic but source type '{1}' is not", 
                                        interfaceType, sourceType));
                }
                sourceType = sourceType.GetGenericTypeDefinition();
                interfaceType = interfaceType.GetGenericTypeDefinition();  
            }

            Type cachedType;
            if (_interface.TryFindTypeInCache(sourceType, interfaceType, out cachedType))
            {
                return cachedType;
            }


            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException("Expecting interface type instaead of " + interfaceType);
            }

            Interlocked.Increment(ref _counter);

            var typeName = "ProxyFor" + sourceType.Name.Replace('`','_') + "_" + _counter;

            AssemblyBuilder ab;
            GenericTypeParameterBuilder[] gtpb;
            var tb = GetTypeBuilder(typeName, sourceType, interfaceType, out ab, out gtpb);

            var fieldBuilder = 
                tb.DefineField("_source", sourceType, 
                                FieldAttributes.Private | 
                                FieldAttributes.InitOnly);

            CreateConstructor(sourceType, tb, fieldBuilder);

            var objectType = typeof (object);
            CreateMethods(interfaceType, objectType, sourceType, tb, fieldBuilder, gtpb);

            CreateProperties(interfaceType, sourceType, tb, fieldBuilder, gtpb);

            CreateEvents(interfaceType, sourceType, tb, fieldBuilder, gtpb);
            

            var type = tb.CreateType();

// TODO: remove, it is here just for testing
ab.Save("Temp" + typeName + ".dll");

            IDictionary<Type, Type> cacheBySourceType;
            if (!_cacheByInterfaceType.TryGetValue(interfaceType, out cacheBySourceType))
            {
                _cacheByInterfaceType[interfaceType] = new Dictionary<Type, Type>();
            }
            _cacheByInterfaceType[interfaceType][sourceType] = type;

            return type;
        }

        private void CreateEvents(Type interfaceType, Type sourceType, TypeBuilder tb, FieldBuilder fieldBuilder, GenericTypeParameterBuilder[] gtpb)
        {
            foreach (var evt in interfaceType.GetEvents())
            {
                var sourceEvent = sourceType.GetEvent(evt.Name);
                if (sourceEvent == null)
                {
                    throw new InvalidOperationException(
                        String.Format("Cannot find interface event '{0}.{1}' in source type '{2}'",
                        interfaceType, evt.Name, sourceType));
                }

                var eventBuilder = 
                    tb.DefineEvent(
                        interfaceType.Name + "." + evt.Name, 
                        EventAttributes.None, evt.EventHandlerType);

                // add method
                var interfaceAddMethod = evt.GetAddMethod();
                var sourceAddMethod = sourceEvent.GetAddMethod();
                MethodBuilder addMthdBldr =
                            tb.DefineMethod(interfaceType.FullName + ".add_" + evt.Name,
                                MethodAttributes.Private |
                                MethodAttributes.NewSlot |
                                MethodAttributes.HideBySig |
                                MethodAttributes.Virtual |
                                MethodAttributes.Final,
                              typeof(void), new[] { evt.EventHandlerType });

                EnsureGenericParameters(interfaceAddMethod, addMthdBldr, interfaceType, gtpb);

                addMthdBldr.DefineParameter(1, ParameterAttributes.None, "value");

                ILGenerator addIl = addMthdBldr.GetILGenerator();

                addIl.Emit(OpCodes.Nop);
                addIl.Emit(OpCodes.Ldarg_0);
                addIl.Emit(OpCodes.Ldfld, fieldBuilder);
                addIl.Emit(OpCodes.Ldarg_1);
                addIl.Emit(OpCodes.Callvirt, sourceAddMethod);
                addIl.Emit(OpCodes.Nop);
                addIl.Emit(OpCodes.Ret);

                eventBuilder.SetAddOnMethod(addMthdBldr);
                tb.DefineMethodOverride(addMthdBldr, interfaceAddMethod);



                // remove method
                var interfaceRemoveMethod = evt.GetRemoveMethod();
                var sourceRemoveMethod = sourceEvent.GetRemoveMethod();

                MethodBuilder removeMthdBldr =
                            tb.DefineMethod(interfaceType.FullName + ".remove_" + evt.Name,
                                MethodAttributes.Private |
                                MethodAttributes.NewSlot |
                                MethodAttributes.HideBySig |
                                MethodAttributes.Virtual |
                                MethodAttributes.Final,
                              typeof(void), new[] { evt.EventHandlerType });

                EnsureGenericParameters(interfaceRemoveMethod, removeMthdBldr, interfaceType, gtpb);

                removeMthdBldr.DefineParameter(1, ParameterAttributes.None, "value");

                ILGenerator removeIl = removeMthdBldr.GetILGenerator();

                removeIl.Emit(OpCodes.Nop);
                removeIl.Emit(OpCodes.Ldarg_0);
                removeIl.Emit(OpCodes.Ldfld, fieldBuilder);
                removeIl.Emit(OpCodes.Ldarg_1);
                removeIl.Emit(OpCodes.Callvirt, sourceRemoveMethod);
                removeIl.Emit(OpCodes.Nop);
                removeIl.Emit(OpCodes.Ret);

                eventBuilder.SetRemoveOnMethod(removeMthdBldr);


                tb.DefineMethodOverride(removeMthdBldr, interfaceRemoveMethod);
            }
        }

        private void CreateProperties(Type interfaceType, Type sourceType, TypeBuilder tb, FieldBuilder fieldBuilder, GenericTypeParameterBuilder[] gtpb)
        {
            foreach (var prop in interfaceType.GetProperties())
            {

                PropertyInfo sourceProp = null;
                // indexer
                if (prop.Name == "Item")
                {
                    sourceProp = FindProperty(
                                    sourceType,
                                    prop.Name, 
                                    prop.PropertyType, 
                                    prop.GetIndexParameters()
                                        .Select(pi => pi.ParameterType)
                                        .ToArray());
                }
                else
                {
                    sourceProp = 
                        sourceType.GetProperty(prop.Name);
                }

                if (sourceProp == null)
                {
                    throw new InvalidOperationException(
                        String.Format("Cannot find interface property '{0}.{1}' in source type '{2}'",
                        interfaceType, prop.Name, sourceType));
                }

                var setPars = new List<Type>();
                var getPars = new List<Type>();
                var propInxs = prop.GetIndexParameters();
                if (propInxs.Length > 0)
                {
                    getPars.AddRange(propInxs.Select(pi => pi.ParameterType));
                    setPars.AddRange(propInxs.Select(pi => pi.ParameterType));
                }
                setPars.Add(prop.PropertyType);

                PropertyBuilder propertyBuilder =
                    tb.DefineProperty(
                        interfaceType.Name + "." + prop.Name, PropertyAttributes.SpecialName, prop.PropertyType, 
                        propInxs.Select(pi => pi.ParameterType).ToArray());

                foreach (var accessor in prop.GetAccessors())
                {
                    // if setter
                    if (accessor.ReturnType == typeof (void))
                    {
                        var propParsArray = setPars.ToArray();

                        MethodBuilder setPropMthdBldr =
                            tb.DefineMethod(interfaceType.FullName + ".set_" + prop.Name,
                                MethodAttributes.Private |
                                MethodAttributes.SpecialName |
                                MethodAttributes.NewSlot |
                                MethodAttributes.HideBySig |
                                MethodAttributes.Virtual |
                                MethodAttributes.Final,
                              typeof(void), propParsArray);

                        


                        var sourceAccessor =
                            sourceProp.GetAccessors().FirstOrDefault(a => a.ReturnType == typeof(void));

                        if (sourceAccessor == null)
                        {
                            throw new InvalidOperationException(
                                String.Format("Cannot find set property '{0}.{1}' in source type '{2}'",
                                interfaceType, prop.Name, sourceType));
                        }

                        EnsureGenericParameters(accessor, setPropMthdBldr, interfaceType, gtpb);

                        for (var i = 1; i <= propParsArray.Length; ++i)
                        {
                            var attr =
                                (i - 1) < propInxs.Length
                                    ? propInxs[i - 1].Attributes
                                    : ParameterAttributes.None;

                            setPropMthdBldr.DefineParameter(i, attr, "p" + (i - 1));
                        }

                        ILGenerator setIl = setPropMthdBldr.GetILGenerator();

                        setIl.Emit(OpCodes.Ldarg_0);
                        setIl.Emit(OpCodes.Ldfld, fieldBuilder);
                        setIl.Emit(OpCodes.Ldarg_1);
                        for (var i = 0; i < propInxs.Length; ++i)
                        {
                            if (i == 0) setIl.Emit(OpCodes.Ldarg_2);
                            else if (i == 1) setIl.Emit(OpCodes.Ldarg_3);
                            else setIl.Emit(OpCodes.Ldarg_S, (byte)(i + 2));
                        }
                        setIl.Emit(OpCodes.Callvirt, sourceAccessor);
                        setIl.Emit(OpCodes.Nop);
                        setIl.Emit(OpCodes.Ret);

                        propertyBuilder.SetSetMethod(setPropMthdBldr);

                        tb.DefineMethodOverride(setPropMthdBldr, accessor);
                    }
                    else // if getter
                    {
                        var getParsArray = getPars.ToArray();

                        MethodBuilder getPropMthdBldr =
                            tb.DefineMethod(interfaceType.FullName + ".get_" + prop.Name,
                                MethodAttributes.Private |
                                MethodAttributes.SpecialName |
                                MethodAttributes.NewSlot |
                                MethodAttributes.HideBySig |
                                MethodAttributes.Virtual |
                                MethodAttributes.Final,
                                prop.PropertyType, getParsArray);

                        var sourceAccessor =
                            sourceProp.GetAccessors().FirstOrDefault(a => a.ReturnType != typeof (void));

                        if (sourceAccessor == null)
                        {
                            throw new InvalidOperationException(
                                String.Format("Cannot find get property '{0}.{1}' in source type '{2}'",
                                interfaceType, prop.Name, sourceType));
                        }

                        EnsureGenericParameters(accessor, getPropMthdBldr, interfaceType, gtpb);


                        for (var i = 1; i <= getParsArray.Length; ++i)
                        {
                            var attr = propInxs[i - 1].Attributes;

                            getPropMthdBldr.DefineParameter(i, attr, "p" + (i - 1));
                        }

                        ILGenerator getIl = getPropMthdBldr.GetILGenerator();
                        var lbl = getIl.DefineLabel();

                        getIl.DeclareLocal(prop.PropertyType);
                        getIl.Emit(OpCodes.Ldarg_0);
                        getIl.Emit(OpCodes.Ldfld, fieldBuilder);
                        for (var i = 0; i < propInxs.Length; ++i)
                        {
                            if (i == 0) getIl.Emit(OpCodes.Ldarg_1);
                            else if (i == 1) getIl.Emit(OpCodes.Ldarg_2);
                            else if (i == 2) getIl.Emit(OpCodes.Ldarg_3);
                            else getIl.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                        }
                        getIl.Emit(OpCodes.Callvirt, sourceAccessor);
                        getIl.Emit(OpCodes.Stloc_0);
                        getIl.Emit(OpCodes.Br_S, lbl);
                        getIl.MarkLabel(lbl);
                        getIl.Emit(OpCodes.Ldloc_0);
                        getIl.Emit(OpCodes.Ret);

                        propertyBuilder.SetGetMethod(getPropMthdBldr);

                        tb.DefineMethodOverride(getPropMthdBldr, accessor);
                    }
                }
            }
        }

        private static PropertyInfo FindProperty(Type sourceType, string name, Type propertyType, Type[] toArray)
        {
            foreach (var prop in sourceType.GetProperties())
            {
                if (prop.Name != name) continue;
                var hasExpectedReturnType = 
                    ((propertyType.IsGenericParameter && prop.PropertyType.IsGenericParameter && propertyType.Name == prop.PropertyType.Name) ||
                        propertyType == prop.PropertyType);
                if (!hasExpectedReturnType) continue;
                var sourceParameters = prop.GetIndexParameters().Select(pi => pi.ParameterType).ToArray();
                if (sourceParameters.Length != toArray.Length) continue;
                var continueNext = false;
                for(var i = 0; i < sourceParameters.Length; ++i)
                {
                    var pt = sourceParameters[i];
                    var pte = toArray[i];
                    var hasExpectedParameterType =
                        ((pt.IsGenericParameter && pte.IsGenericParameter &&
                          pt.Name == pte.Name) ||
                         pt == pte);
                    if (!hasExpectedParameterType)
                    {
                        continueNext = true;
                        break;
                    }
                }
                if(continueNext) continue;
                return prop;
            }
            return null;
        }


        private void CreateMethods(Type interfaceType, Type objectType, Type sourceType, TypeBuilder tb, FieldBuilder fieldBuilder, GenericTypeParameterBuilder[] gtpb)
        {
            foreach (var method in interfaceType.GetMethods())
            {
                if (method.DeclaringType == objectType)
                {
                    continue;
                }
                // in case of property accessors
                if (method.IsSpecialName)
                {
                    continue;
                }
                var parameterTypes = method.GetParameters().Select(pi => pi.ParameterType).ToArray();
                var sourceMethod = FindMethod(sourceType, method);
                if (sourceMethod == null)
                {
                    throw new InvalidOperationException("Cannot find method " + method.Name + " in type " + sourceType);
                }


                var currMethod =
                    tb.DefineMethod(interfaceType.FullName + "." + method.Name,
                                    MethodAttributes.Private |
                                    MethodAttributes.HideBySig |
                                    MethodAttributes.NewSlot |
                                    MethodAttributes.Virtual |
                                    MethodAttributes.Final,
                                    CallingConventions.HasThis,
                                    method.ReturnType,
                                    parameterTypes
                        );

                EnsureGenericParameters(method, currMethod, interfaceType, gtpb);

                for (var i = 1; i <= method.GetParameters().Length; ++i)
                {
                    currMethod.DefineParameter(i, method.GetParameters()[i - 1].Attributes, "p" + (i - 1));
                }

                var methodIl = currMethod.GetILGenerator();
                var lbl = methodIl.DefineLabel();
                if (method.ReturnType != typeof(void))
                {
                    methodIl.DeclareLocal(method.ReturnType);
                }
                methodIl.Emit(OpCodes.Nop);
                methodIl.Emit(OpCodes.Ldarg_0);
                methodIl.Emit(OpCodes.Ldfld, fieldBuilder);

                var ps = method.GetParameters();
                for (var i = 0; i < ps.Length; ++i)
                {
                    if (i == 0) methodIl.Emit(OpCodes.Ldarg_1);
                    else if (i == 1) methodIl.Emit(OpCodes.Ldarg_2);
                    else if (i == 2) methodIl.Emit(OpCodes.Ldarg_3);
                    else methodIl.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                }
                methodIl.Emit(OpCodes.Callvirt, sourceMethod);
                if (method.ReturnType == typeof(void))
                {
                    methodIl.Emit(OpCodes.Nop);
                }
                else
                {
                    methodIl.Emit(OpCodes.Stloc_0);
                    methodIl.Emit(OpCodes.Br_S, lbl);
                    methodIl.MarkLabel(lbl);
                    methodIl.Emit(OpCodes.Ldloc_0);
                }
                methodIl.Emit(OpCodes.Ret);

                tb.DefineMethodOverride(currMethod, method);
            }
        }

        private static void EnsureGenericParameters(MethodInfo method, MethodBuilder currMethod, Type interfaceType, IEnumerable<GenericTypeParameterBuilder> gtpb)
        {
            if (method.ContainsGenericParameters)
            {
                var genericArgsList = method.GetGenericArguments().ToList();

                var methodGenerics = 
                    genericArgsList.Count == 0 
                        ? new List<GenericTypeParameterBuilder>()
                        : currMethod.DefineGenericParameters(genericArgsList.Select(g => g.Name).ToArray()).ToList();

                if (gtpb != null)
                {
                    genericArgsList.AddRange(interfaceType.GetGenericArguments());
                    methodGenerics.AddRange(gtpb);
                }
                var genericArguments = genericArgsList.ToArray();

                GenericTypeParameterBuilder[] genParams = methodGenerics.ToArray();

                List<Type> interfaceConstraints = null;
                for (int i = 0; i < genParams.Length; i++)
                {
                    genParams[i].SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);

                    foreach (Type constraint in genericArguments[i].GetGenericParameterConstraints())
                    {
                        if (constraint.IsClass)
                            genParams[i].SetBaseTypeConstraint(constraint);
                        else
                        {
                            if (interfaceConstraints == null)
                                interfaceConstraints = new List<Type>();
                            interfaceConstraints.Add(constraint);
                        }
                    }

                    if (interfaceConstraints != null && interfaceConstraints.Count != 0)
                    {
                        genParams[i].SetInterfaceConstraints(interfaceConstraints.ToArray());
                        interfaceConstraints.Clear();
                    }
                }
            }
        }

        private MethodInfo FindMethod(Type sourceType, MethodInfo methodInfo)
        {
            foreach (var m in sourceType.GetMethods())
            {
                if (m.Name != methodInfo.Name) continue;
                var expectedParams = methodInfo.GetParameters();
                var parameters = m.GetParameters();
                if (expectedParams.Length != parameters.Length) continue;
                var continueNext = false;
                for (var i = 0; i < expectedParams.Length; ++i)
                {
                    var e = expectedParams[i];
                    var r = parameters[i];
                    if (e.IsIn != r.IsIn ||
                        e.IsOut != r.IsOut ||
                        e.IsRetval != r.IsRetval ||
                        e.IsOptional != r.IsOptional ||
                        e.ParameterType.Name != r.ParameterType.Name)
                    {
                        continueNext = true;
                        break;
                    }
                }
                if (continueNext)
                {
                    continue;
                }
                return m;
            }
            return null;
        }

        private static void CreateConstructor(Type sourceType, TypeBuilder tb, FieldBuilder fieldBuilder)
        {
            var constructorBuilder =
                tb.DefineConstructor(MethodAttributes.Public |
                                     MethodAttributes.HideBySig |
                                     MethodAttributes.SpecialName |
                                     MethodAttributes.RTSpecialName,
                                     CallingConventions.HasThis,
                                     new[]{ sourceType });


            constructorBuilder.DefineParameter(1, ParameterAttributes.None, "source");


            ILGenerator constrIl = constructorBuilder.GetILGenerator();
            constrIl.Emit(OpCodes.Ldarg_0);
            var objectConstructor = typeof(object).GetConstructors().First();
            constrIl.Emit(OpCodes.Call, objectConstructor);
            constrIl.Emit(OpCodes.Nop);
            constrIl.Emit(OpCodes.Nop);
            constrIl.Emit(OpCodes.Ldarg_0);
            constrIl.Emit(OpCodes.Ldarg_1);
            constrIl.Emit(OpCodes.Stfld, fieldBuilder);
            constrIl.Emit(OpCodes.Nop);
            constrIl.Emit(OpCodes.Ret);
        }

        private TypeBuilder GetTypeBuilder(string typeName, Type sourceType, Type interfaceType, out AssemblyBuilder ab, out GenericTypeParameterBuilder[] gtpb)
        {
            var an = new AssemblyName
            {
                Name = "Temp" + typeName + ".dll",
                Version = new Version(1, 0, 0, 0)
            };
// TODO: change to Run
            ab =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                //        an, AssemblyBuilderAccess.Run);
                  an, AssemblyBuilderAccess.RunAndSave);//change to test saving assembly

            var moduleBuilder = ab.DefineDynamicModule(an.Name);





            var tb = moduleBuilder.DefineType("DynamicProxyTypes." + typeName, 
                                TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.Sealed |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit
                                , typeof(object), new[] { interfaceType });



            var interfaceGenericTypes = interfaceType.GetGenericArguments();
            var sourceGenericTypes = sourceType.GetGenericArguments();
            if (interfaceGenericTypes.Length != sourceGenericTypes.Length)
            {
                throw new InvalidOperationException(
                    String.Format("Number of generic types (#{0}) of interface '{1}' does not match number of generic types (#{2}) of source type '{3}'",
                    interfaceGenericTypes.Length, interfaceGenericTypes,
                    sourceGenericTypes.Length, sourceGenericTypes));
            }

            gtpb =
                interfaceGenericTypes.Length > 0
                    ? tb.DefineGenericParameters(interfaceGenericTypes.Select(t => t.Name).ToArray())
                    : null;

            return tb;
        }

    }
}