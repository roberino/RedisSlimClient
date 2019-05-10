using System;
using System.Collections.Generic;
using System.Text;

namespace RedisSlimClient.Serialization.Emit
{
    class EnumerableBuilderStrategy
    {
        Type _listInterface = typeof(IList<>);

        public Func<object> CreateEnumerableStrategy(Type type)
        {
            if (type.IsArray)
            {

            }

            return () => new List<string>();
        }
    }
}
