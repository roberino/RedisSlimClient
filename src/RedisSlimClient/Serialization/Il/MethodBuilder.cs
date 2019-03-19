using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RedisSlimClient.Serialization.Il
{
    internal class MethodBuilder
    {
        readonly IDictionary<string, (int index, Type type, bool isParam)> _locals;

        int _localCounter;

        public MethodBuilder(
            TypeBuilder newType,
            MethodInfo method)
        {
            TargetMethod = method;
            Parameters = method.GetParameters();

            var getMethod =
                newType.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType,
                    Parameters.Select(p => p.ParameterType).ToArray());

            Il = getMethod.GetILGenerator();

            _locals = new Dictionary<string, (int index, Type type, bool isParam)>();

            var i = 1;

            foreach (var parameter in method.GetParameters())
            {
                _locals[parameter.Name] = (i++, parameter.ParameterType, true);
            }
        }

        public MethodInfo TargetMethod { get; }

        public IReadOnlyCollection<ParameterInfo> Parameters { get; }

        public ILGenerator Il { get; }

        public override string ToString()
        {
            return Il.ToString();
        }

        public LocalVar GetLocalByIndex(int index)
        {
            var lv = _locals.First(x => x.Value.index == index && !x.Value.isParam);

            return new LocalVar(lv.Key, lv.Value.type) {Index = index};
        }

        public LocalVar Define(Type type, bool initialise = false, string name = null)
        {
            if (name == null)
            {
                name = $"$_{_localCounter++}";
            }

            if (!name.StartsWith("$"))
            {
                throw new ArgumentException("Variables must start with $");
            }

            ConstructorInfo returnTypeConstructor = null;

            if (initialise)
            {
                returnTypeConstructor = type.GetConstructors().FirstOrDefault(ctr => ctr.GetParameters().Length == 0);
            }

            var local = Il.DeclareLocal(type);

            _locals[name] = (local.LocalIndex, type, false);

            if (initialise)
            {
                Il.Emit(OpCodes.Newobj, returnTypeConstructor);
                Il.Emit(OpCodes.Stloc, local.LocalIndex);
            }

            return new LocalVar(name, type) {Index = local.LocalIndex };
        }

        public void CallMethod(object target, MethodInfo method, params object[] parameters)
        {
            CallMethod(null, target, method, parameters);
        }

        public void CallFunction(LocalVar outputLocal, object target, MethodInfo method, params object[] parameters)
        {
            CallMethod(outputLocal, target, method, parameters);
        }

        public void CallStaticFunction(LocalVar outputLocal, MethodInfo method, params object[] parameters)
        {
            CallFunction(outputLocal, null, method, parameters);
        }

        public void Add(LocalVar outputVar, object left, object right)
        {
            EmitValue(left, typeof(int));
            EmitValue(right, typeof(int));

            Il.Emit(OpCodes.Add);
            Il.Emit(OpCodes.Stloc, outputVar.Index);
        }

        public void Return(object value = null)
        {
            EmitValue(value);
            Il.Emit(OpCodes.Ret);
        }

        public void Scope(Action action)
        {
            Il.BeginScope();

            action();

            Il.EndScope();
        }

        void CallMethod(LocalVar localStore, object target, MethodInfo method, params object[] parameters)
        {
            if (target != null)
            {
                EmitValue(target, method.DeclaringType);
            }

            foreach (var item in parameters.Zip(method.GetParameters(), (local, parameter) => (local, parameter)))
            {
                EmitValue(item.local, item.parameter.ParameterType);
            }

            var op = method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;

            Il.EmitCall(op, method, null);

            if (localStore != null)
            {
                if (method.ReturnType == typeof(void))
                {
                    throw new ArgumentException();
                }

                var localInf = _locals[localStore.Name];

                if (localInf.type.IsClass && method.ReturnType.IsValueType)
                {
                    Il.Emit(OpCodes.Box, localInf.type);
                }
                
                Il.Emit(OpCodes.Stloc, localInf.index);
            }
        }

        void EmitValue(object value, Type requiredType = null)
        {
            switch (value)
            {
                case null:
                    return;
                case string str:
                    Il.Emit(OpCodes.Ldstr, str);
                    return;
                case int i:
                    Il.Emit(OpCodes.Ldc_I4, i);
                    return;
                case long l:
                    Il.Emit(OpCodes.Ldc_I8, l);
                    return;
                case ParameterInfo p:
                    EmitLocal(_locals[p.Name], requiredType);
                    return;
            }

            if (!(value is LocalVar lv)) throw new NotSupportedException(value.GetType().ToString());

            if (lv.IsNull)
            {
                return;
            }

            EmitLocal(_locals[lv.Name], requiredType);
        }

        void EmitLocal((int index, Type type, bool isParam) localInf, Type requiredType)
        {
            var code = localInf.isParam ? OpCodes.Ldarg : OpCodes.Ldloc;
            Il.Emit(code, localInf.index);

            if (requiredType != null && localInf.type != requiredType)
            {
                Il.Emit(OpCodes.Castclass, requiredType);
            }
        }
    }
}