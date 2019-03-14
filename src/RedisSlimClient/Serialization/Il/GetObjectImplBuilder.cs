using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
{
    class GetObjectImplBuilder<T> : SerializeMethodImplBuilder<T>
    {
        readonly IDictionary<Type, (MethodInfo prop, MethodInfo meth)> _extractMethods;
        readonly MethodInfo _addMethod;

        public GetObjectImplBuilder(TypeBuilder newType, IEnumerable<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.GetObjectData)), properties)
        {   
            var listType = typeof(Dictionary<string, object>);

            _addMethod = listType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m =>
                m.Name == nameof(Dictionary<string, object>.Add) && m.GetParameters().Length == 2);

            _extractMethods = new Dictionary<Type, (MethodInfo prop, MethodInfo meth)>();
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
        }

        protected override void OnProperty(MethodBuilder methodBuilder, LocalVar propertyLocal, PropertyInfo property)
        {
            if (property.PropertyType.RequiresDecomposition())
            {
                var extract = GetExtractor(property.PropertyType);

                methodBuilder.Scope(() =>
                {
                    var extractLocal = methodBuilder.Define(extract.prop.ReturnType);
                    methodBuilder.CallStaticFunction(extractLocal, extract.prop);
                    methodBuilder.CallFunction(propertyLocal, extractLocal, extract.meth, propertyLocal);
                });
            }

            var x = methodBuilder.GetLocalByIndex(0);

            methodBuilder.CallMethod(x, _addMethod, property.Name, propertyLocal);
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
    }
}