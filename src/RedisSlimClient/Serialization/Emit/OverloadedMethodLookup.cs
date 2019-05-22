using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedisSlimClient.Serialization.Emit
{
    static class OverloadedMethodLookupExtensions
    {
        public static OverloadedMethodLookup<T, Type> CreateParameterOverload<T>(string methodName, string overloadedParameterName)
        {
            return new OverloadedMethodLookup<T, Type>(methodName,
                m => m.GetParameters().Single(p => p.Name == overloadedParameterName).ParameterType)
            {
                DefaultBinding = x => x.methodType != typeof(object) && x.methodType.IsAssignableFrom(x.targetType),
                FallbackBinding = x => x.methodType == typeof(object)
            };
        }
    }

    class OverloadedMethodLookup<T, TKey>
    {
        readonly IDictionary<TKey, MethodInfo> _methods;

        public OverloadedMethodLookup(string methodName, Func<MethodInfo, TKey> grouping)
            : this(m => m.Name == methodName, grouping)
        {
        }

        public OverloadedMethodLookup(Func<MethodInfo, bool> methodFilter, Func<MethodInfo, TKey> grouping)
        {
            var type = typeof(T);

            _methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(methodFilter)
                .GroupBy(grouping)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public MethodInfo BindByGenericParam(Func<ParameterInfo, bool> genericParamFilter, params Type[] typeArgs)
        {
            var genMethod = _methods.Values
                .FirstOrDefault(m => 
                    m.IsGenericMethod 
                    && m.ContainsGenericParameters
                    && m.GetParameters().Where(p => p.ParameterType.ContainsGenericParameter()).Any(genericParamFilter));

            if (genMethod != null)
            {
                return genMethod.MakeGenericMethod(typeArgs);
            }

            throw new InvalidOperationException();
        }

        public MethodInfo BindByGenericReturnValue(Func<Type, bool> returnTypeFilter, params Type[] typeArgs)
        {
            var genMethod = _methods.Values
                .FirstOrDefault(m =>
                    m.IsGenericMethod
                    && returnTypeFilter(m.ReturnType));

            if (genMethod != null)
            {
                return genMethod.MakeGenericMethod(typeArgs);
            }

            throw new InvalidOperationException();
        }

        public Func<(TKey targetType, TKey methodType), bool> DefaultBinding { get; set; }

        public Func<(TKey targetType, TKey methodType), bool> FallbackBinding { get; set; }

        public MethodInfo Bind(TKey type)
        {
            if (_methods.TryGetValue(type, out var method))
            {
                return method;
            }

            if (DefaultBinding != null)
            {
                var assignable = _methods.FirstOrDefault(kv => DefaultBinding.Invoke((type, kv.Key)));

                if (assignable.Value != null)
                {
                    return assignable.Value;
                }
            }

            return _methods.SingleOrDefault(kv => FallbackBinding.Invoke((type, kv.Key))).Value;
        }
    }
}