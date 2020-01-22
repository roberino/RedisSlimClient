using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisTribute.Serialization.Emit
{
    abstract class TypeProxyBuilder<T>
    {
        readonly MethodBuilder _methodWriter;
        readonly IDictionary<Type, (MethodInfo prop, MethodInfo meth)> _extractMethods;

        protected TypeProxyBuilder(TypeBuilder newType, MethodInfo method, IReadOnlyCollection<MemberInfo> properties)
        {
            _methodWriter = new MethodBuilder(newType, method);
            _extractMethods = new Dictionary<Type, (MethodInfo prop, MethodInfo meth)>();

            Members = properties;
        }

        protected MethodInfo TargetMethod => _methodWriter.TargetMethod;

        protected IReadOnlyCollection<MemberInfo> Members { get; }

        protected virtual void OnProperty(MethodBuilder methodBuilder, PropertyInfo property) { }

        protected virtual void OnField(MethodBuilder methodBuilder, FieldInfo field) { }

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

            foreach (var property in Members)
            {
                _methodWriter.Scope(() =>
                {
                    if (property is PropertyInfo p)
                    {
                        OnProperty(_methodWriter, p);
                        return;
                    }

                    if (property is FieldInfo f)
                    {
                        OnField(_methodWriter, f);
                        return;
                    }
                });
            }

            OnFinalize(_methodWriter);

            OnReturn(_methodWriter, returnLocal);
        }

        protected virtual void OnReturn(MethodBuilder methodBuilder, LocalVar returnLocal)
        {
            methodBuilder.Return(returnLocal);
        }
    }
}