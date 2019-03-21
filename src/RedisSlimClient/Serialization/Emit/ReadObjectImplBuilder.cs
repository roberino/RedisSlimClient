using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Emit
{
    internal class ReadObjectImplBuilder<T> : TypeProxyBuilder<T>
    {
        readonly OverloadedMethodLookup<IObjectReader, Type> _objectReaderMethods;
        readonly MethodInfo _endReadMethod;
        readonly ParameterInfo _readerParam;
        readonly ParameterInfo _targetParam;

        public ReadObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter<T>).GetMethod(nameof(IObjectGraphExporter<T>.WriteObjectData)), properties)
        {
            var objectReaderType = typeof(IObjectReader);
            _objectReaderMethods = new OverloadedMethodLookup<IObjectReader, Type>(m => m.Name.StartsWith(nameof(IObjectReader.ReadChar).Substring(0, 4)), x => x.ReturnType);
            _endReadMethod = objectReaderType.GetMethod(nameof(IObjectReader.EndRead), BindingFlags.Public | BindingFlags.Instance);

            var paramz = TargetMethod.GetParameters();

            _targetParam = paramz[0];
            _readerParam = paramz[1];
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
        }

        protected override void OnProperty(MethodBuilder methodBuilder, PropertyInfo property)
        {
            var propertyLocal = methodBuilder.Define(property.GetMethod.ReturnType);

            MethodInfo readMethod;

            if (property.PropertyType.RequiresDecomposition())
            {
                var collectionType = property.PropertyType.CollectionType();

                if (collectionType == null)
                {
                    LoadExtractor(property.PropertyType);

                    readMethod = _objectReaderMethods
                        .BindToGeneric(p => p.ParameterType.IsGenericParameter, property.PropertyType);
                }
                else
                {
                    LoadExtractor(collectionType);

                    var targetType = typeof(IEnumerable<>);

                    readMethod = _objectReaderMethods
                        .BindToGeneric(p => p.ParameterType.IsGenericType 
                                            && p.ParameterType.GetGenericTypeDefinition() == targetType, collectionType);
                }
            }
            else
            {
                readMethod = _objectReaderMethods.Bind(property.PropertyType);
            }

            methodBuilder.CallFunction(propertyLocal,
                _readerParam,
                readMethod,
                property.Name);

            methodBuilder.CallMethod(_targetParam, property.SetMethod, propertyLocal);
        }

        void LoadExtractor(Type type)
        {
            GetTypeModelMethod(type, nameof(TypeProxy<object>.WriteData));
        }
    }
}