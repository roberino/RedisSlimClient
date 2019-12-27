using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisTribute.Serialization.Emit
{
    class ReadObjectImplBuilder<T> : TypeProxyBuilder<T>
    {
        readonly OverloadedMethodLookup<IObjectReader, Type> _objectReaderMethods;
        readonly Type _objectReaderType;
        readonly ParameterInfo _readerParam;
        readonly ParameterInfo _targetParam;

        public ReadObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter<T>).GetMethod(nameof(IObjectGraphExporter<T>.ReadObjectData)), properties)
        {
            _objectReaderType = typeof(IObjectReader);
            _objectReaderMethods = new OverloadedMethodLookup<IObjectReader, Type>("Read", x => x.ReturnType);

            var paramz = TargetMethod.GetParameters();

            _targetParam = paramz[0];
            _readerParam = paramz[1];
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
            var endMethod = GetMethod(nameof(IObjectReader.BeginRead));
            methodBuilder.CallMethod(_readerParam, endMethod, Properties.Count);
        }

        protected override void OnFinalize(MethodBuilder methodBuilder)
        {
            var endMethod = GetMethod(nameof(IObjectReader.EndRead));
            methodBuilder.CallMethod(_readerParam, endMethod);
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
                       .BindByGenericReturnValue(nameof(IObjectReader.ReadObject), t => t.IsGenericParameter, property.PropertyType);
                }
                else
                {
                    LoadExtractor(collectionType);

                    var targetType = typeof(IEnumerable<>);

                    readMethod = _objectReaderMethods
                        .BindByGenericReturnValue(nameof(IObjectReader.ReadEnumerable), t => t.IsGenericType && t.GetGenericTypeDefinition() == targetType, 
                            collectionType);
                }

                methodBuilder.CallFunction(propertyLocal, _targetParam, property.GetMethod);

                methodBuilder.CallFunction(propertyLocal,
                    _readerParam,
                    readMethod,
                    property.Name,
                    propertyLocal);
            }
            else
            {
                if (property.PropertyType.IsEnum)
                {
                    readMethod = _objectReaderMethods
                        .BindGenericByMethod(m => m.Name == nameof(IObjectReader.ReadEnum), property.PropertyType);

                    methodBuilder.CallFunction(propertyLocal, _targetParam, property.GetMethod);

                    methodBuilder.CallFunction(propertyLocal,
                        _readerParam,
                        readMethod,
                        property.Name,
                        propertyLocal);
                }
                else
                {
                    if (property.PropertyType.IsNullableType())
                    {
                        var innerType = Nullable.GetUnderlyingType(property.PropertyType);

                        readMethod = _objectReaderMethods.Bind(innerType);

                        methodBuilder.CallFunction(propertyLocal,
                            _readerParam,
                            readMethod,
                            property.Name);

                        var ctor = property.PropertyType.GetConstructor(new[] { innerType });

                        methodBuilder.Il.Emit(OpCodes.Ldloc, propertyLocal.Index);

                        methodBuilder.Il.Emit(OpCodes.Newobj, ctor);

                        methodBuilder.Il.Emit(OpCodes.Stloc, propertyLocal.Index);
                    }
                    else
                    {
                        readMethod = _objectReaderMethods.Bind(property.PropertyType);

                        methodBuilder.CallFunction(propertyLocal,
                            _readerParam,
                            readMethod,
                            property.Name);
                    }
                }
            }

            methodBuilder.CallMethod(_targetParam, property.SetMethod, propertyLocal);
        }

        MethodInfo GetMethod(string name) =>
            _objectReaderType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        void LoadExtractor(Type type)
        {
            GetTypeModelMethod(type, nameof(TypeProxy<object>.WriteData));
        }
    }
}