using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
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

        protected abstract void OnProperty(MethodBuilder methodBuilder, LocalVar propertyLocal, PropertyInfo property);

        protected abstract void OnInit(MethodBuilder methodBuilder);

        protected (MethodInfo prop, MethodInfo meth) GetTypeModelMethod(Type type, string methodName)
        {
            if (!_extractMethods.TryGetValue(type, out var extractMethods))
            {
                var m = typeof(TypeModel<>).MakeGenericType(type);

                var instanceProp = m.GetProperty(nameof(TypeModel<object>.Instance), BindingFlags.Public | BindingFlags.Static);
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

            var arg0 = TargetMethod.GetParameters().First();

            OnInit(_methodWriter);

            foreach (var property in Properties)
            {
                _methodWriter.Scope(() =>
                {
                    var propertyLocal = _methodWriter.Define(property.GetMethod.ReturnType);

                    _methodWriter.CallFunction(propertyLocal, arg0, property.GetMethod);

                    OnProperty(_methodWriter, propertyLocal, property);
                });
            }

            _methodWriter.Return(returnLocal);
        }
    }
}