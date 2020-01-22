using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly IList<LocalVar> _locals;
        readonly bool _byConstructor;

        public ReadObjectImplBuilder(TypeBuilder newType, IReadOnlyCollection<MemberInfo> members, bool byConstructor = false)
            : base(newType, typeof(IObjectGraphExporter<T>).GetMethod(nameof(IObjectGraphExporter<T>.ReadObjectData)), members)
        {
            _objectReaderType = typeof(IObjectReader);
            _objectReaderMethods = new OverloadedMethodLookup<IObjectReader, Type>("Read", x => x.ReturnType);

            _locals = new List<LocalVar>();

            var paramz = TargetMethod.GetParameters();

            _targetParam = paramz[0];
            _readerParam = paramz[1];
            _byConstructor = byConstructor;
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
            var endMethod = GetMethod(nameof(IObjectReader.BeginRead));
            methodBuilder.CallMethod(_readerParam, endMethod, Members.Count);
        }

        protected override void OnFinalize(MethodBuilder methodBuilder)
        {
            var endMethod = GetMethod(nameof(IObjectReader.EndRead));
            methodBuilder.CallMethod(_readerParam, endMethod);
        }

        protected override void OnReturn(MethodBuilder methodBuilder, LocalVar returnLocal)
        {
            if (_byConstructor)
            {
                methodBuilder.NewObj(typeof(T), _locals.ToArray());
                methodBuilder.Return();
            }
            else
            {
                methodBuilder.Return(_targetParam);
            }
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
            var propertyLocal = methodBuilder.Define(member.Type);

            _locals.Add(propertyLocal);

            MethodInfo readMethod;

            void LoadValue()
            {
                if (member.HasGetMethod)
                {
                    methodBuilder.CallFunction(propertyLocal, _targetParam, member.GetMethod);
                }
                else
                {
                    if (member.IsField)
                    {
                        methodBuilder.AccessField(propertyLocal, (FieldInfo)member.Member, _targetParam);
                    }
                }
            }

            if (member.Type.RequiresDecomposition())
            {
                var collectionType = member.Type.CollectionType();

                if (collectionType == null)
                {
                    LoadExtractor(member.Type);

                    readMethod = _objectReaderMethods
                       .BindByGenericReturnValue(nameof(IObjectReader.ReadObject), t => t.IsGenericParameter, member.Type);
                }
                else
                {
                    LoadExtractor(collectionType);

                    var targetType = typeof(IEnumerable<>);

                    readMethod = _objectReaderMethods
                        .BindByGenericReturnValue(nameof(IObjectReader.ReadEnumerable), t => t.IsGenericType && t.GetGenericTypeDefinition() == targetType,
                            collectionType);
                }

                LoadValue();

                methodBuilder.CallFunction(propertyLocal,
                    _readerParam,
                    readMethod,
                    member.Name,
                    propertyLocal);
            }
            else
            {
                if (member.Type.IsEnum)
                {
                    readMethod = _objectReaderMethods
                        .BindGenericByMethod(m => m.Name == nameof(IObjectReader.ReadEnum), member.Type);

                    LoadValue();

                    methodBuilder.CallFunction(propertyLocal,
                        _readerParam,
                        readMethod,
                        member.Name,
                        propertyLocal);
                }
                else
                {
                    if (member.Type.IsNullableType())
                    {
                        var innerType = Nullable.GetUnderlyingType(member.Type);

                        readMethod = _objectReaderMethods.Bind(innerType);

                        methodBuilder.CallFunction(propertyLocal,
                            _readerParam,
                            readMethod,
                            member.Name);

                        var ctor = member.Type.GetConstructor(new[] { innerType });

                        methodBuilder.Il.Emit(OpCodes.Ldloc, propertyLocal.Index);

                        methodBuilder.Il.Emit(OpCodes.Newobj, ctor);

                        methodBuilder.Il.Emit(OpCodes.Stloc, propertyLocal.Index);
                    }
                    else
                    {
                        readMethod = _objectReaderMethods.Bind(member.Type);

                        methodBuilder.CallFunction(propertyLocal,
                            _readerParam,
                            readMethod,
                            member.Name);
                    }
                }
            }

            if (member.CanSet && !_byConstructor)
            {
                if (member.IsField)
                {
                    methodBuilder.SetField((FieldInfo)member.Member, propertyLocal);
                }

                else
                {
                    methodBuilder.CallMethod(_targetParam, member.SetMethod, propertyLocal);
                }
            }
        }

        MethodInfo GetMethod(string name) =>
            _objectReaderType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        void LoadExtractor(Type type)
        {
            GetTypeModelMethod(type, nameof(TypeProxy<object>.WriteData));
        }
    }
}