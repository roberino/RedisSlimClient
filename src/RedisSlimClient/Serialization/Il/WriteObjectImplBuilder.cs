using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
{
    class WriteObjectImplBuilder<T> : SerializeMethodImplBuilder<T>
    {
        private readonly OverloadedMethodLookup<IObjectWriter> _objectWriterMethods;
        private readonly MethodInfo _beginWriteMethod;
        private readonly ParameterInfo _writerParam;

        LocalVar _counter;

        public WriteObjectImplBuilder(TypeBuilder newType, IEnumerable<PropertyInfo> properties)
            : base(newType, typeof(IObjectGraphExporter).GetMethod(nameof(IObjectGraphExporter.WriteObjectData)), properties)
        {
            var objectWriterType = typeof(IObjectWriter);
            _objectWriterMethods = new OverloadedMethodLookup<IObjectWriter>(nameof(IObjectWriter.WriteItem), 2);
            _beginWriteMethod = objectWriterType.GetMethod(nameof(IObjectWriter.BeginWrite), BindingFlags.Public | BindingFlags.Instance);
            _writerParam = TargetMethod.GetParameters().ElementAt(1);
        }

        protected override void OnInit(MethodBuilder methodBuilder)
        {
            methodBuilder.CallMethod(_writerParam, _beginWriteMethod, Properties.Count());

            _counter = methodBuilder.Define(typeof(int));

            methodBuilder.Add(_counter, methodBuilder.Parameters.ElementAt(2), _counter);
        }

        protected override void OnProperty(MethodBuilder methodBuilder, LocalVar propertyLocal, PropertyInfo property)
        {
            if (property.PropertyType.RequiresDecomposition())
            {
            }

            var writeMethod = _objectWriterMethods.Bind(property.PropertyType);

            methodBuilder.CallMethod(_writerParam,
                writeMethod,
                property.Name, _counter, propertyLocal);
        }
    }
}