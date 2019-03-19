using System;
using System.Collections;
using System.Collections.Generic;
using RedisSlimClient.Types;

namespace RedisSlimClient.Serialization
{
    internal class ByteReader : IEnumerable<RedisObjectPart>, IDisposable
    {
        readonly IEnumerable<ArraySegment<byte>> _byteStream;

        ReadState _currentState;
        (ResponseType type, long length, int offset) _currentType;
        int _arrayIndex;
        long? _currentArrayLength;

        public ByteReader(IEnumerable<ArraySegment<byte>> byteStream)
        {
            _byteStream = byteStream;
            _currentState = ReadState.Type;
            _currentType = (ResponseType.Unknown, 0, 0);
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
                            OpenArray(_currentType.length);
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

        void OpenArray(long length)
        {
            _currentState = ReadState.Type;
            _currentArrayLength = length;
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
                Length = _currentArrayLength.Value
            };

            if (_arrayIndex == _currentArrayLength.Value)
            {
                _currentArrayLength = null;
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