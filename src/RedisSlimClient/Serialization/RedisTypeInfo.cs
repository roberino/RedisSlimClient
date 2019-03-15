using System;

namespace RedisSlimClient.Serialization
{
    internal static class RedisTypeInfo
    {
        public static bool RequiresDecomposition(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.Object && type != typeof(byte[]);
        }
    }
}
