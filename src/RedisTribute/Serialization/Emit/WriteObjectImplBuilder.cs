using System;
using System.Collections.Generic;
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

        public WriteObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<MemberInfo> members)
            : base(newType, typeof(IObjectGraphExporter<T>).GetMethod(nameof(IObjectGraphExporter<T>.WriteObjectData)), members)
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
            methodBuilder.CallMethod(_writerParam, _beginWriteMethod, Members.Count);
        }

        protected override void OnFinalize(MethodBuilder methodBuilder)
        {
        }

        protected override void OnField(MethodBuilder methodBuilder, FieldInfo field)
        {
            OnMember(methodBuilder, new MemberFacade(field));
        }

        protected override void OnProperty(MethodBuilder methodBuilder, PropertyInfo property)
        {
            OnMember(methodBuilder, new MemberFacade(property));
        }

        void OnMember(MethodBuilder methodBuilder, MemberFacade member)
        {
            var propertyValueLocal = methodBuilder.Define(member.Type);

            void LoadValue()
            {
                if (member.HasGetMethod)
                {
                    methodBuilder.CallFunction(propertyValueLocal, _targetParam, member.GetMethod);
                }
                else
                {
                    if (member.IsField)
                    {
                        methodBuilder.AccessField(propertyValueLocal, (FieldInfo)member.Member, _targetParam);
                    }
                }
            }

            LoadValue();

            MethodInfo writeMethod;

            if (member.Type.RequiresDecomposition())
            {
                var collectionType = member.Type.CollectionType();

                if (collectionType == null)
                {
                    LoadExtractor(member.Type);

                    writeMethod = _objectWriterMethods
                       .BindByGenericParam(nameof(IObjectWriter.WriteItem), p => p.ParameterType.IsGenericParameter, member.Type);
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
                if (member.Type.IsEnum)
                {
                    writeMethod = _objectWriterMethods
                        .BindGenericByMethod(m => m.Name == nameof(IObjectWriter.WriteEnum), member.Type);
                }
                else
                {
                    if (member.Type.IsNullableType())
                    {
                        var ut = Nullable.GetUnderlyingType(member.Type);

                        var nullWriteMethod = _objectWriterMethods.BindGenericByMethod(m => m.Name == nameof(IObjectWriter.WriteNullable), ut);

                        writeMethod = _objectWriterMethods.Bind(ut);

                        methodBuilder.CallMethod(_writerParam, nullWriteMethod, member.Name, propertyValueLocal, writeMethod.Name);

                        return;
                    }
                    else
                    {
                        writeMethod = _objectWriterMethods.Bind(member.Type);
                    }
                }
            }

            if (writeMethod == null)
            {
                throw new ArgumentException(member.Type.FullName);
            }

            methodBuilder.CallMethod(_writerParam,
                writeMethod,
                member.Name, propertyValueLocal);
        }

        void LoadExtractor(Type type)
        {
            GetTypeModelMethod(type, nameof(TypeProxy<object>.WriteData));
        }
    }
}