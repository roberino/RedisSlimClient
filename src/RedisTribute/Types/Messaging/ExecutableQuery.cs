using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisTribute.Types.Messaging
{
    class ExecutableQuery<T> : IMessageData
    {
        private readonly Func<IRedisReader, IEnumerable<T>> _query;

        public ExecutableQuery(Func<IRedisReader, IEnumerable<T>> query)
        {
            _query = query;
        }

        public static ExecutableQuery<T> FromBytes(byte[] data)
        {
            CreateAssembly(b =>
            {

            });

            throw new NotImplementedException();
        }

        public string Channel { get; }

        public byte[] GetBytes()
        {
            var body = _query.Method; 
            
            return body.GetMethodBody().GetILAsByteArray();
        }

        static Type CreateAssembly(Action<MethodBuilder> build)
        {
            var assemblyName = new AssemblyName { Name = "Rand_Sz" };

            var asm = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var module = asm.DefineDynamicModule("Module");

            var type = module.DefineType("Query", TypeAttributes.Public);

            var methodBuilder = module.DefineGlobalMethod(nameof(ExecutableQuery<object>), MethodAttributes.Public, typeof(IEnumerable<T>), new[] { typeof(IRedisReader) });

            build(methodBuilder);

            return type.CreateTypeInfo().AsType();
        }
    }
}
