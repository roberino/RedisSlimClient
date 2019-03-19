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
            var x = methodBuilder.GetLocalByIndex(0);

            if (property.PropertyType.RequiresDecomposition())
            {
                var extract = GetExtractor(property.PropertyType);

                var extractLocal = methodBuilder.Define(extract.prop.ReturnType);
                var extractedDataLocal = methodBuilder.Define(typeof(Dictionary<string, object>));

                methodBuilder.CallStaticFunction(extractLocal, extract.prop);
                methodBuilder.CallFunction(extractedDataLocal, extractLocal, extract.meth, propertyLocal);

                methodBuilder.CallMethod(x, _addMethod, property.Name, extractedDataLocal);
            }
            else
            {
                methodBuilder.CallMethod(x, _addMethod, property.Name, propertyLocal);
            }
        }

        (MethodInfo prop, MethodInfo meth) GetExtractor(Type type)
        {
            return GetTypeModelMethod(type, nameof(TypeProxy<object>.GetData));
        }
    }
}