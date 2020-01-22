using System;
using System.Reflection;

namespace RedisTribute.Serialization.Emit
{
    class MemberFacade
    {
        public MemberFacade(FieldInfo field)
        {
            Member = field;
            Type = field.FieldType;
            Name = field.Name;
            IsField = true;
            CanSet = !field.IsInitOnly;
        }

        public MemberFacade(PropertyInfo property)
        {
            Member = property;
            Type = property.PropertyType;
            Name = property.Name;
            GetMethod = property.GetMethod;
            SetMethod = property.SetMethod;
            HasGetMethod = true;
            CanSet = property.CanWrite;
        }

        public Type Type { get; }

        public string Name { get; }

        public bool CanSet { get; }

        public MethodInfo GetMethod { get; }
        public MethodInfo SetMethod { get; }

        public bool HasGetMethod { get; }

        public bool IsField { get; }

        public MemberInfo Member { get; }

    }
}
