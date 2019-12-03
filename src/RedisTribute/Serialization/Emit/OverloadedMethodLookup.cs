using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedisTribute.Serialization.Emit
{
    static class OverloadedMethodLookupExtensions
    {
        public static OverloadedMethodLookup<T, Type> CreateParameterOverload<T>(string methodNamePrefix, string overloadedParameterName)
        {
            return new OverloadedMethodLookup<T, Type>(methodNamePrefix,
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

        public OverloadedMethodLookup(string methodNamePrefix, Func<MethodInfo, TKey> grouping)
            : this(m => m.Name.StartsWith(methodNamePrefix), grouping)
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

        public MethodInfo BindGenericByMethod(Func<MethodInfo, bool> genericParamFilter, params Type[] typeArgs)
        {
            var genMethod = _methods.Values
                .FirstOrDefault(genericParamFilter);

            if (genMethod != null)
            {
                return genMethod.MakeGenericMethod(typeArgs);
            }

            throw new InvalidOperationException();
        }

        public MethodInfo BindToGenericParam(string methodName, params Type[] typeArgs)
        {
            return BindGenericByMethod(m =>
            m.Name == methodName
            && m.IsGenericMethod
            && m.ContainsGenericParameters
            && m.GetParameters().Where(p => p.ParameterType.ContainsGenericParameter()).Any(p => p.ParameterType.IsGenericType), typeArgs);
        }

        public MethodInfo BindByGenericParam(string methodName, Func<ParameterInfo, bool> genericParamFilter, params Type[] typeArgs)
        {
            var genMethod = _methods.Values
                .FirstOrDefault(m =>
                    m.Name == methodName
                    && m.IsGenericMethod
                    && m.ContainsGenericParameters
                    && m.GetParameters().Where(p => p.ParameterType.ContainsGenericParameter()).Any(genericParamFilter));

            if (genMethod != null)
            {
                return genMethod.MakeGenericMethod(typeArgs);
            }

            throw new InvalidOperationException();
        }

        public MethodInfo BindByGenericReturnValue(string methodName, Func<Type, bool> returnTypeFilter, params Type[] typeArgs)
        {
            var genMethod = _methods.Values
                .FirstOrDefault(m =>
                    m.Name == methodName
                    && m.IsGenericMethod
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

            if (FallbackBinding == null)
            {
                return null;
            }

            return _methods.SingleOrDefault(kv => FallbackBinding.Invoke((type, kv.Key))).Value;
        }
    }
}