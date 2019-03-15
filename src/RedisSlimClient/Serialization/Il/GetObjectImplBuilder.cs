using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
{
    internal class GetObjectImplBuilder<T> : TypeProxyBuilder<T>
    {
        readonly MethodInfo _addMethod;

        public GetObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.GetObjectData)), properties)
        {   
            var listType = typeof(Dictionary<string, object>);

            _addMethod = listType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m =>
                m.Name == nameof(Dictionary<string, object>.Add) && m.GetParameters().Length == 2);
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
            return GetTypeModelMethod(type, nameof(TypeModel<object>.GetData));
        }
    }
}