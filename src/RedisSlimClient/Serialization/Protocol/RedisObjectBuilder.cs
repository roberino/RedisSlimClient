using RedisSlimClient.Types;
using RedisSlimClient.Types.Primatives;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Serialization.Protocol
{
    class RedisObjectBuilder
    {
        readonly ByteSequenceParser _parser;
        readonly List<RedisObjectPart> _items;
        readonly IRedisObject[] _empty = new IRedisObject[0];

        public RedisObjectBuilder()
        {
            _parser = new ByteSequenceParser();
            _items = new List<RedisObjectPart>();
        }

        public IRedisObject[] AppendObjectData(ReadOnlySequence<byte> obj)
        {
            var byteSequence = new MemoryByteSequenceAdapter(obj);

            _items.AddRange(_parser.ReadItem(byteSequence));

            if (_items.Count > 0)
            {
                var objs = _items.ToObjects().ToArray();

                if (objs.Length > 0 && objs.Last().IsComplete)
                {
                    _items.Clear();
                    return objs;
                }
            }

            return _empty;
        }
    }
}