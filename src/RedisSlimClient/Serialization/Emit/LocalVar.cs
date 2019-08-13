using System;

namespace RedisSlimClient.Serialization.Emit
{
    class LocalVar
    {
        public LocalVar(string name = null, Type type = null)
        {
            Name = name;
            Type = type;
        }

        public static readonly LocalVar Null = new LocalVar();

        public bool IsNull => Name == null;
        public string Name { get; }
        public Type Type { get; }
        public int Index { get; set; }
    }
}
