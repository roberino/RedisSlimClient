using System;
using System.Collections.Generic;
using System.Text;
using RedisSlimClient.Types;

namespace RedisSlimClient.Serialization
{
    class ObjectConverter
    {
        public RedisObject Convert(object obj)
        {
            if (obj == null)
            {
                return new RedisString(new byte[0]);
            }

            var tc = Type.GetTypeCode(obj.GetType());

            return null;
        }

        public RedisInteger Convert(long value)
        {
            return new RedisInteger(value);
        }
    }
}
