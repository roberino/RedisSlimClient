using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
{
    abstract class SerializeMethodImplBuilder<T>
    {
        readonly TypeBuilder _newType;
        readonly IEnumerable<PropertyInfo> _properties;
        readonly MethodBuilder _methodWriter;

        protected SerializeMethodImplBuilder(TypeBuilder newType, MethodInfo method, IEnumerable<PropertyInfo> properties)
        {
            _newType = newType;
            _properties = properties;

            _methodWriter = new MethodBuilder(_newType, method);
        }

        protected MethodInfo TargetMethod => _methodWriter.TargetMethod;

        protected IEnumerable<PropertyInfo> Properties => _properties;

        protected abstract void OnProperty(MethodBuilder methodBuilder, LocalVar propertyLocal, PropertyInfo property);

        protected abstract void OnInit(MethodBuilder methodBuilder);

        public void Build()
        {
            var isVoid = TargetMethod.ReturnType == typeof(void);

            var returnLocal = LocalVar.Null;

            if (!isVoid)
            {
                returnLocal = _methodWriter.Define(TargetMethod.ReturnType, true);
            }

            var propertyLocal = _methodWriter.Define(typeof(object));
            var arg0 = TargetMethod.GetParameters().First();

            OnInit(_methodWriter);

            foreach (var property in _properties)
            {
                _methodWriter.CallFunction(propertyLocal, arg0, property.GetMethod);

                OnProperty(_methodWriter, propertyLocal, property);
            }

            _methodWriter.Return(returnLocal);
        }
    }
}