using RedisSlimClient.Serialization.Il;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace RedisSlimClient.Serialization
{
    public sealed class TypeModel<T>
    {
        static readonly string TypeName = $"{typeof(T).Namespace}.{typeof(T).Name}.TypeReader";

        private static readonly Lazy<TypeModel<T>> SingleInstance =
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

        public void WriteData(T instance, IObjectWriter writer) => _dataExtractor.WriteObjectData(instance, writer, 0);

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

        void AddPropertyExtractMethod(SerializeMethodImplBuilder<T> builder)
        {
            var targetMethod = typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.WriteObjectData));
            var objectWriterType = typeof(IObjectWriter);
            var objectWriterMethods = new OverloadedMethodLookup<>(objectWriterType, nameof(IObjectWriter.WriteItem), 2);

            LocalVar counter = null;

            //builder.AddPropertyReaderMethod(targetMethod, (il, propLocal, prop) =>
            //    {
            //        var writeMethod = objectWriterMethods.Bind(prop.PropertyType);

            //        il.CallMethod(targetMethod.GetParameters().ElementAt(1),
            //            writeMethod,
            //            prop.Name, counter, propLocal);
            //    },
            //    il =>
            //    {
            //        counter = il.CreateLocal(typeof(int));

            //        il.Add(counter, targetMethod.GetParameters().ElementAt(2), counter);
            //    });
        }
    }
}