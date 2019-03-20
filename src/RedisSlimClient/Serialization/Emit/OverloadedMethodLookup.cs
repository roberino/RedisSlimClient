using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedisSlimClient.Serialization.Emit
{
    internal class OverloadedMethodLookup<T>
    {
        readonly IDictionary<Type, MethodInfo> _methods;

        public OverloadedMethodLookup(string methodName, string overloadedParameterName)
        {
            var type = typeof(T);

            _methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == methodName)
                .GroupBy(m => m.GetParameters().Single(p => p.Name == overloadedParameterName).ParameterType)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public MethodInfo BindToGeneric(Func<ParameterInfo, bool> genericParamFilter, params Type[] typeArgs)
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

        public MethodInfo Bind(Type type)
        {
            if (_methods.TryGetValue(type, out var method))
            {
                return method;
            }

            var assignable = _methods.FirstOrDefault(kv => kv.Key != typeof(object) && kv.Key.IsAssignableFrom(type));

            if (assignable.Value != null)
            {
                return assignable.Value;
            }

            return _methods.SingleOrDefault(kv => kv.Key == typeof(object)).Value;
        }
    }
}