using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Emit
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
                var collectionType = property.PropertyType.CollectionType();

                if (collectionType == null)
                {
                    GetExtractor(property.PropertyType);

                    var writeMethod = _objectWriterMethods
                        .BindToGeneric(p => p.ParameterType.IsGenericParameter, property.PropertyType);

                    methodBuilder.CallMethod(_writerParam,
                        writeMethod,
                        property.Name, propertyLocal);

                    //var extract = GetExtractor(property.PropertyType);

                    //var extractLocal = methodBuilder.Define(extract.prop.ReturnType);
                    //methodBuilder.CallStaticFunction(extractLocal, extract.prop);
                    //methodBuilder.CallMethod(extractLocal, extract.meth, propertyLocal, _writerParam);
                }
                else
                {
                    GetExtractor(collectionType);

                    var targetType = typeof(IEnumerable<>);

                    var writeMethod = _objectWriterMethods
                        .BindToGeneric(p => p.ParameterType.IsGenericType 
                                            && p.ParameterType.GetGenericTypeDefinition() == targetType, collectionType);
                    
                    methodBuilder.CallMethod(_writerParam,
                        writeMethod,
                        property.Name, propertyLocal);
                }
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