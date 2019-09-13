using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisTribute.Serialization.Emit
{
    abstract class TypeProxyBuilder<T>
    {
        readonly MethodBuilder _methodWriter;
        readonly IDictionary<Type, (MethodInfo prop, MethodInfo meth)> _extractMethods;

        protected TypeProxyBuilder(TypeBuilder newType, MethodInfo method, IReadOnlyCollection<PropertyInfo> properties)
        {
            _methodWriter = new MethodBuilder(newType, method);
            _extractMethods = new Dictionary<Type, (MethodInfo prop, MethodInfo meth)>();

            Properties = properties;
        }

        protected MethodInfo TargetMethod => _methodWriter.TargetMethod;

        protected IReadOnlyCollection<PropertyInfo> Properties { get; }

        protected abstract void OnProperty(MethodBuilder methodBuilder, PropertyInfo property);

        protected abstract void OnInit(MethodBuilder methodBuilder);

        protected abstract void OnFinalize(MethodBuilder methodBuilder);

        protected (MethodInfo prop, MethodInfo meth) GetTypeModelMethod(Type type, string methodName)
        {
            if (!_extractMethods.TryGetValue(type, out var extractMethods))
            {
                var m = typeof(TypeProxy<>).MakeGenericType(type);

                var instanceProp = m.GetProperty(nameof(TypeProxy<object>.Instance), BindingFlags.Public | BindingFlags.Static);
                var instance = instanceProp.GetValue(null);
                var method = instance.GetType().GetMethod(methodName);

                _extractMethods[type] = extractMethods = (instanceProp.GetMethod, method);
            }

            return extractMethods;
        }

        public void Build()
        {
            var isVoid = TargetMethod.ReturnType == typeof(void);

            var returnLocal = LocalVar.Null;

            if (!isVoid)
            {
                returnLocal = _methodWriter.Define(TargetMethod.ReturnType, true);
            }

            OnInit(_methodWriter);

            foreach (var property in Properties)
            {
                _methodWriter.Scope(() =>
                {
                    OnProperty(_methodWriter, property);
                });
            }

            OnFinalize(_methodWriter);

            _methodWriter.Return(returnLocal);
        }
    }
}