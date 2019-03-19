using RedisSlimClient.Serialization.Il;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace RedisSlimClient.Serialization
{
    public interface ITypeModel<T>
    {
        void WriteData(T instance, IObjectWriter writer);
    }

    public sealed class TypeModel<T> : ITypeModel<T>
    {
        static readonly string TypeName = $"{typeof(T).Namespace}.{typeof(T).Name}.TypeReader";

        static readonly Lazy<TypeModel<T>> SingleInstance =
            new Lazy<TypeModel<T>>(() => new TypeModel<T>(), LazyThreadSafetyMode.ExecutionAndPublication);

        readonly IObjectGraphExporter _dataExtractor;

        TypeModel()
        {
            TargetType = typeof(T);

            var newType = CreateAssembly();

            _dataExtractor = Activator.CreateInstance(newType) as IObjectGraphExporter;
        }

        public static TypeModel<T> Instance => SingleInstance.Value;

        public Type TargetType { get; }

        public IDictionary<string, object> GetData(T instance) => _dataExtractor.GetObjectData(instance);

        public void WriteData(T instance, IObjectWriter writer) => _dataExtractor.WriteObjectData(instance, writer);

        /// <summary>
        /// Create an assembly that will provide the get and set methods.
        /// </summary>
        Type CreateAssembly()
        {
            var assemblyName = new AssemblyName {Name = TargetType.Name + "_Sz"};

            var asm = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var module = asm.DefineDynamicModule("Module");

            var type = BuildType(module);

            return type.AsType();
        }

        TypeInfo BuildType(ModuleBuilder newModule)
        {
            var newAccessorType = newModule.DefineType(TypeName, TypeAttributes.Public);

            newAccessorType.AddInterfaceImplementation(typeof(IObjectGraphExporter));

            newAccessorType.DefineDefaultConstructor(MethodAttributes.Public);

            var targetProps = TargetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();

            new GetObjectImplBuilder<T>(newAccessorType, targetProps).Build();
            new WriteObjectImplBuilder<T>(newAccessorType, targetProps).Build();
            
            return newAccessorType.CreateTypeInfo();
        }
    }
}