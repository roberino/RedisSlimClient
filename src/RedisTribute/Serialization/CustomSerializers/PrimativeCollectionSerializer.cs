using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RedisTribute.Serialization.CustomSerializers
{
    class PrimativeCollectionSerializer<T> : IObjectSerializer<IEnumerable<T>>
    {
        readonly IBinaryConverter<T> _converter;

        public PrimativeCollectionSerializer()
        {
            _converter = PrimativeSerializer.CreateConverter<T>();
        }

        public IEnumerable<T> ReadData(IObjectReader reader, IEnumerable<T> defaultValue)
        {
            throw new NotImplementedException();
        }

        public void WriteData(IEnumerable<T> instance, IObjectWriter writer)
        {

        }
    }
}