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
        readonly RedisObject[] _empty = new RedisObject[0];

        public RedisObjectBuilder()
        {
            _parser = new ByteSequenceParser();
            _items = new List<RedisObjectPart>();
        }

        public RedisObject[] AppendObjectData(ReadOnlySequence<byte> obj)
        {
            var byteSequence = new MemoryByteSequenceAdapter(obj);

            _items.AddRange(_parser.ReadItem(byteSequence));

            var objs = _items.ToObjects().ToArray();

            if (objs.Last().IsComplete)
            {
                return objs;
            }

            return _empty;
        }
    }
}