using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace RedisSlimClient.Serialization.Emit
{
    sealed class TypeProxy<T> : IObjectSerializer<T>
    {
        static readonly string TypeName = $"{typeof(T).Namespace}.{typeof(T).Name}.TypeProxy";

        static readonly Lazy<TypeProxy<T>> SingleInstance =
            new Lazy<TypeProxy<T>>(() => new TypeProxy<T>(), LazyThreadSafetyMode.ExecutionAndPublication);

        readonly IObjectGraphExporter<T> _dataExtractor;

        TypeProxy()
        {
            TargetType = typeof(T);

            var newType = CreateAssembly();

            _dataExtractor = Activator.CreateInstance(newType) as IObjectGraphExporter<T>;
        }

        public static TypeProxy<T> Instance => SingleInstance.Value;

        public Type TargetType { get; }

        public void WriteData(T instance, IObjectWriter writer) => _dataExtractor.WriteObjectData(instance, writer);

        public T ReadData(IObjectReader reader, T defaultValue)
        {
            T newObject;

            if (defaultValue == null)
            {
                newObject = (T)typeof(T).GetConstructor(Type.EmptyTypes)?.Invoke(new object[0]);
            }
            else
            {
                newObject = defaultValue;
            }

            _dataExtractor.ReadObjectData(newObject, reader);

            return newObject;
        }

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

            newAccessorType.AddInterfaceImplementation(typeof(IObjectGraphExporter<T>));

            newAccessorType.DefineDefaultConstructor(MethodAttributes.Public);

            var targetProps = TargetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();

            new WriteObjectImplBuilder<T>(newAccessorType, targetProps).Build();
            new ReadObjectImplBuilder<T>(newAccessorType, targetProps).Build();

            return newAccessorType.CreateTypeInfo();
        }
    }
}