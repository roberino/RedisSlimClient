using System;
using System.Collections;
using System.Collections.Generic;
using RedisSlimClient.Types;

namespace RedisSlimClient.Serialization
{
    internal class RedisSequenceReader : IEnumerable<RedisObjectPart>, IDisposable
    {
        readonly IEnumerable<ArraySegment<byte>> _byteStream;
        readonly Stack<int> _currentArrayIndex;

        ReadState _currentState;
        (ResponseType type, long length, int offset) _currentType;
        int _level;
        int _arrayIndex;
        long? _currentArrayLength;
        
        public RedisSequenceReader(IEnumerable<ArraySegment<byte>> byteStream)
        {
            _byteStream = byteStream;
            _currentState = ReadState.Type;
            _currentType = (ResponseType.Unknown, 0, 0);
            _currentArrayIndex = new Stack<int>();
        }

        public IEnumerator<RedisObjectPart> GetEnumerator()
        {
            foreach (var segment in _byteStream)
            {
                if (_currentState == ReadState.Type)
                {
                    _currentType = segment.ToResponseType();

                    switch (_currentType.type)
                    {
                        case ResponseType.BulkStringType:
                            _currentState = ReadState.Value;
                            continue;
                        case ResponseType.ArrayType:
                            yield return OpenArray(_currentType.length);
                            continue;
                        case ResponseType.IntType:
                        {
                            var part = YieldObjectPart(new RedisInteger(_currentType.length));

                            if (!part.IsEmpty)
                            {
                                yield return part;
                            }

                            continue;
                        }
                    }
                }

                {
                    var part = YieldObjectPart(GetCurrentValue(segment));

                    if (!part.IsEmpty)
                    {
                        yield return part;
                    }
                }
            }
        }

        RedisObjectPart OpenArray(long length)
        {
            _currentState = ReadState.Type;
            _currentArrayLength = length;

            if (_level > 0)
            {
                _currentArrayIndex.Push(_arrayIndex);
            }

            var array = new RedisObjectPart()
            {
                IsArrayStart = true,
                Length = length,
                Level = _level++,
                ArrayIndex = _arrayIndex
            };

            _arrayIndex = 0;

            return array;
        }

        RedisObjectPart YieldObjectPart(RedisObject value)
        {
            _currentState = ReadState.Type;
            
            if (!_currentArrayLength.HasValue || value == null)
            {
                return new RedisObjectPart
                {
                    Value = value
                };
            }

            var item = new RedisObjectPart
            {
                Value = value,
                ArrayIndex = _arrayIndex++,
                Level = _level,
                Length = _currentArrayLength.Value
            };

            if (_arrayIndex == _currentArrayLength.Value)
            {
                _currentArrayLength = null;
                _level--;

                if (_currentArrayIndex.Count > 0)
                {
                    _arrayIndex = _currentArrayIndex.Pop();
                }
            }

            return item;
        }

        RedisObject GetCurrentValue(ArraySegment<byte> segment)
        {
            switch (_currentType.type)
            {
                case ResponseType.ErrorType:
                    return new RedisError(segment.ToAsciiString(_currentType.offset));
                case ResponseType.StringType:
                    return new RedisString(segment.ToBytes(_currentType.offset));
                case ResponseType.BulkStringType:
                    return new RedisString(segment.ToBytes(_currentType.offset));
            }

            throw new NotSupportedException(_currentType.type.ToString());
        }

        public void Dispose()
        {
        }

        enum ReadState
        {
            Type,
            Value
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}