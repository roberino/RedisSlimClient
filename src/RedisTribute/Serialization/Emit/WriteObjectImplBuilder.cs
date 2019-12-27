using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisTribute.Serialization.Emit
{
    class WriteObjectImplBuilder<T> : TypeProxyBuilder<T>
    {
        readonly OverloadedMethodLookup<IObjectWriter, Type> _objectWriterMethods;
        readonly MethodInfo _beginWriteMethod;
        readonly ParameterInfo _writerParam;
        readonly ParameterInfo _targetParam;

        public WriteObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter<T>).GetMethod(nameof(IObjectGraphExporter<T>.WriteObjectData)), properties)
        {
            var objectWriterType = typeof(IObjectWriter);
            _objectWriterMethods = OverloadedMethodLookupExtensions.CreateParameterOverload<IObjectWriter>("Write", "data");
            _beginWriteMethod = objectWriterType.GetMethod(nameof(IObjectWriter.BeginWrite), BindingFlags.Public | BindingFlags.Instance);

            var paramz = TargetMethod.GetParameters();

            _targetParam = paramz[0];
            _writerParam = paramz[1];
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
            methodBuilder.CallMethod(_writerParam, _beginWriteMethod, Properties.Count);
        }

        protected override void OnFinalize(MethodBuilder methodBuilder)
        {
        }

        protected override void OnProperty(MethodBuilder methodBuilder, PropertyInfo property)
        {
            var propertyValueLocal = methodBuilder.Define(property.GetMethod.ReturnType);

            methodBuilder.CallFunction(propertyValueLocal, _targetParam, property.GetMethod);

            MethodInfo writeMethod;

            if (property.PropertyType.RequiresDecomposition())
            {
                var collectionType = property.PropertyType.CollectionType();

                if (collectionType == null)
                {
                    LoadExtractor(property.PropertyType);

                    writeMethod = _objectWriterMethods
                       .BindByGenericParam(nameof(IObjectWriter.WriteItem), p => p.ParameterType.IsGenericParameter, property.PropertyType);
                }
                else
                {
                    LoadExtractor(collectionType);

                    var targetType = typeof(IEnumerable<>);

                    writeMethod = _objectWriterMethods
                        .BindByGenericParam(nameof(IObjectWriter.WriteItem), p => p.ParameterType.IsGenericType 
                                            && p.ParameterType.GetGenericTypeDefinition() == targetType, collectionType);
                    }
            }
            else
            {
                if (property.PropertyType.IsEnum)
                {
                    writeMethod = _objectWriterMethods
                        .BindGenericByMethod(m => m.Name == nameof(IObjectWriter.WriteEnum), property.PropertyType);
                }
                else
                {
                    if (property.PropertyType.IsNullableType())
                    {
                        var ut = Nullable.GetUnderlyingType(property.PropertyType);

                        var nullWriteMethod = _objectWriterMethods.BindGenericByMethod(m => m.Name == nameof(IObjectWriter.WriteNullable), ut);

                        writeMethod = _objectWriterMethods.Bind(ut);

                        methodBuilder.CallMethod(_writerParam, nullWriteMethod, property.Name, propertyValueLocal, writeMethod.Name);

                        return;
                    }
                    else
                    {
                        writeMethod = _objectWriterMethods.Bind(property.PropertyType);
                    }
                }
            }

            if (writeMethod == null)
            {
                throw new ArgumentException(property.PropertyType.FullName);
            }

            methodBuilder.CallMethod(_writerParam,
                writeMethod,
                property.Name, propertyValueLocal);
        }

        void LoadExtractor(Type type)
        {
            GetTypeModelMethod(type, nameof(TypeProxy<object>.WriteData));
        }
    }
}