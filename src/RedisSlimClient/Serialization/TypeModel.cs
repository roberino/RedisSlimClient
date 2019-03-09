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

            AddPropertyReadMethod(newAccessorType, targetProps);
            AddPropertyExtractMethod(newAccessorType, targetProps);

            return newAccessorType.CreateTypeInfo();
        }

        void AddPropertyReadMethod(TypeBuilder newType, IEnumerable<PropertyInfo> properties)
        {
            var builder = new TypeModelBuilder<T>();
            var targetMethod = typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.GetObjectData));

            var listType = typeof(Dictionary<string, object>);
            var listAddMethod = listType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m => m.Name == nameof(Dictionary<string, object>.Add) && m.GetParameters().Length == 2);

            builder.AddPropertyReaderMethod(newType, targetMethod, properties, (il, prop) =>
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldstr, prop.Name);
                il.Emit(OpCodes.Ldloc_1);

                il.EmitCall(OpCodes.Call, listAddMethod, null);
            });
        }

        void AddPropertyExtractMethod(TypeBuilder newType, IEnumerable<PropertyInfo> properties)
        {
            var builder = new TypeModelBuilder<T>();
            var targetMethod = typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.WriteObjectData));
            var objectWriterType = typeof(IObjectWriter);
            var objectWriterMethod = objectWriterType.GetMethod(nameof(IObjectWriter.WriteItem));

            var localIntIndex = 0;

            builder.AddPropertyReaderMethod(newType, targetMethod, properties, (il, prop) =>
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.Emit(OpCodes.Ldloc, localIntIndex);
                    il.Emit(OpCodes.Ldloc_1);

                    il.EmitCall(OpCodes.Call, objectWriterMethod, null);
                },
                il =>
                {
                    var localInt = il.DeclareLocal(typeof(int));

                    localIntIndex = localInt.LocalIndex;

                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldc_I4, 1);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, localIntIndex);
                });
        }
    }
}