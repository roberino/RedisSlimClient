using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization
{
    class TypeModelBuilder<T>
    {
        readonly IDictionary<Type, (MethodInfo prop, MethodInfo meth)> _extractMethods;

        public TypeModelBuilder()
        {
            _extractMethods = new Dictionary<Type, (MethodInfo prop, MethodInfo meth)>();
        }

        (MethodInfo prop, MethodInfo meth) GetExtractor(Type type)
        {
            if (!_extractMethods.TryGetValue(type, out var extractMethods))
            {
                var m = typeof(TypeModel<>).MakeGenericType(type);

                var instanceProp = m.GetProperty(nameof(TypeModel<object>.Instance), BindingFlags.Public | BindingFlags.Static);
                var instance = instanceProp.GetValue(null);
                var method = instance.GetType().GetMethod(nameof(TypeModel<object>.GetData));

                _extractMethods[type] = extractMethods = (instanceProp.GetMethod, method);
            }

            return extractMethods;
        }

        public void AddPropertyReaderMethod(
            TypeBuilder newType,
            MethodInfo method,
            IEnumerable<PropertyInfo> properties,
            Action<ILGenerator, PropertyInfo> propertyBuilder,
            Action<ILGenerator> initialise = null)
        {
            var getMethod =
                newType.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType,
                    method.GetParameters().Select(p => p.ParameterType).ToArray());

            var il = getMethod.GetILGenerator();

            var isVoid = method.ReturnType == typeof(void);

            if (!isVoid)
            {
                var returnTypeConstructor =
                    method.ReturnType.GetConstructors().First(ctr => ctr.GetParameters().Length == 0);

                il.DeclareLocal(method.ReturnType);

                il.Emit(OpCodes.Newobj, returnTypeConstructor);
                il.Emit(OpCodes.Stloc_0);
            }

            il.DeclareLocal(typeof(object));

            initialise?.Invoke(il);

            foreach (var property in properties)
            {
                il.Emit(OpCodes.Ldarg_1);

                il.Emit(OpCodes.Castclass, typeof(T));

                il.EmitCall(OpCodes.Call, property.GetMethod, null);

                if (property.GetMethod.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, property.GetMethod.ReturnType);
                }

                if (property.GetMethod.ReturnType.RequiresDecomposition())
                {
                    il.Emit(OpCodes.Stloc_1);

                    il.BeginScope();

                    var extractMethod = GetExtractor(property.GetMethod.ReturnType);

                    var var1 = il.DeclareLocal(extractMethod.prop.ReturnType);

                    il.EmitCall(OpCodes.Call, extractMethod.prop, null);

                    il.Emit(OpCodes.Stloc, var1.LocalIndex);

                    il.Emit(OpCodes.Ldloc, var1.LocalIndex);
                    il.Emit(OpCodes.Ldloc_1);

                    il.EmitCall(OpCodes.Call, extractMethod.meth, null);

                    il.EndScope();
                }

                il.Emit(OpCodes.Stloc_1);

                propertyBuilder(il, property);
            }

            if (!isVoid)
            {
                il.Emit(OpCodes.Ldloc_0);
            }

            il.Emit(OpCodes.Ret);
        }
    }
}