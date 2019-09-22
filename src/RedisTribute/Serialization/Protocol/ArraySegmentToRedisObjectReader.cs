using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RedisTribute.Types;
using RedisTribute.Types.Primatives;

namespace RedisTribute.Serialization
{
    class ArraySegmentToRedisObjectReader : IEnumerable<RedisObjectPart>
    {
        readonly IEnumerable<IByteSequence> _byteStream;
        readonly ByteSequenceParser _parser;

        public ArraySegmentToRedisObjectReader(IEnumerable<IByteSequence> byteStream)
        {
            _byteStream = byteStream;
            _parser = new ByteSequenceParser();
        }

        public ArraySegmentToRedisObjectReader(IEnumerable<ArraySegment<byte>> byteStream) 
            : this(byteStream.Select(b => (IByteSequence)new ArraySegmentByteSequenceAdapter(b)))
        {
        }

        public IEnumerator<RedisObjectPart> GetEnumerator()
        {
            foreach (var segment in _byteStream)
            {
                foreach(var item in _parser.ReadItem(segment))
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}