using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Serialization
{
    internal static class TypeExtensions
    {
        public static bool RequiresDecomposition(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.Object && type != typeof(byte[]);
        }

        public static bool ContainsGenericParameter(this Type type)
        {
            if (type.IsGenericParameter || type.ContainsGenericParameters)
            {
                return true;
            }

            //if (type.IsGenericType && type.GetGenericArguments().Any(a => a.ContainsGenericParameter()))
            //{
            //    return true;
            //}

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