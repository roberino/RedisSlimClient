using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RedisTribute.Serialization
{
    class TypeHeader<T>
    {
        readonly IDictionary<string, (PropertyInfo property, int index)> _properties;

        public TypeHeader()
        {
            TargetType = typeof(T);

            var i = 0;
            _properties = TargetType.SerializableProperties().ToDictionary(p => p.Name, p => (p, i++));
        }

        public Type TargetType { get; }

        public IEnumerable<PropertyInfo> Properties => _properties.Values.Select(p => p.property);

        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(ToString());
        }

        public int GetIndex(string propertyName)
        {
            return _properties[propertyName].index;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(TargetType.FullName);

            return sb.ToString();
        }
    }
}