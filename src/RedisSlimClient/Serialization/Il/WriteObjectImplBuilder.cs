using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
{
    internal class WriteObjectImplBuilder<T> : TypeProxyBuilder<T>
    {
        readonly OverloadedMethodLookup<IObjectWriter> _objectWriterMethods;
        readonly MethodInfo _beginWriteMethod;
        readonly ParameterInfo _writerParam;

        public WriteObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.WriteObjectData)), properties)
        {
            var objectWriterType = typeof(IObjectWriter);
            _objectWriterMethods = new OverloadedMethodLookup<IObjectWriter>(nameof(IObjectWriter.WriteItem), "data");
            _beginWriteMethod = objectWriterType.GetMethod(nameof(IObjectWriter.BeginWrite), BindingFlags.Public | BindingFlags.Instance);
            _writerParam = TargetMethod.GetParameters().ElementAt(1);
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
            methodBuilder.CallMethod(_writerParam, _beginWriteMethod, Properties.Count);
        }

        protected override void OnProperty(MethodBuilder methodBuilder, LocalVar propertyLocal, PropertyInfo property)
        {
            if (property.PropertyType.RequiresDecomposition())
            {
                var extract = GetExtractor(property.PropertyType);

                var extractLocal = methodBuilder.Define(extract.prop.ReturnType);
                methodBuilder.CallStaticFunction(extractLocal, extract.prop);
                methodBuilder.CallMethod(extractLocal, extract.meth, propertyLocal, _writerParam);
            }
            else
            {
                var writeMethod = _objectWriterMethods.Bind(property.PropertyType);

                methodBuilder.CallMethod(_writerParam,
                    writeMethod,
                    property.Name, propertyLocal);
            }
        }

        (MethodInfo prop, MethodInfo meth) GetExtractor(Type type)
        {
            return GetTypeModelMethod(type, nameof(TypeProxy<object>.WriteData));
        }
    }
}