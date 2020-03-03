using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedisTribute.Serialization
{
    static class TypeExtensions
    {
        public static PropertyInfo[] SerializableProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.HasNonSerializeMarker())
                .ToArray();
        }

        public static FieldInfo[] SerializableFields(this Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !p.HasNonSerializeMarker())
                .ToArray();
        }

        public static bool IsValueTuple(this Type type)
        {
            return type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition().Name.Contains("ValueTuple");
        }

        public static bool IsCollectionOrArray(this Type type)
        {
            return type.IsArray || (type.IsClass && type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)));
        }

        public static T? MakeNullable<T>(T value) where T : struct
        {
            return new T?(value);
        }
        public static ConstructorInfo GetNullableCstr(this Type innerType)
        {
            return typeof(Nullable<>).MakeGenericType(innerType).GetConstructor(new[] { innerType });
        }

        static bool HasNonSerializeMarker(this MemberInfo member)
        {
            var attribs = member.GetCustomAttributes();

            return attribs.Any(x =>
            {
                var name = x.GetType().Name;

                return name == nameof(NonSerializedAttribute) || name == "JsonIgnore";
            });
        }

        public static bool RequiresDecomposition(this Type type)
        {
            if (Type.GetTypeCode(type) == TypeCode.Object && type != typeof(byte[]) && type != typeof(TimeSpan))
            {
                if (IsNullableType(type))
                {
                    var inner = Nullable.GetUnderlyingType(type);

                    return RequiresDecomposition(inner);
                }

                return true;
            }

            return false;
        }

        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool ContainsGenericParameter(this Type type)
        {
            if (type.IsGenericParameter || type.ContainsGenericParameters)
            {
                return true;
            }

            return false;
        }

        public static Type CollectionType(this Type type)
        {
            var ienumerables = type.FindInterfaces((i, c) =>
                i.IsGenericType && ReferenceEquals(i.GetGenericTypeDefinition(), c), typeof(IEnumerable<>));

            if (ienumerables.Any())
            {
                return ienumerables.First().GetGenericArguments().First();
            }

            return null;
        }
    }
}