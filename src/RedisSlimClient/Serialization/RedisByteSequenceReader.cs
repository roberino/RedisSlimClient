using System;
using System.Collections;
using System.Collections.Generic;
using RedisSlimClient.Types;

namespace RedisSlimClient.Serialization
{
    internal class RedisByteSequenceReader : IEnumerable<RedisObjectPart>, IDisposable
    {
        readonly IEnumerable<ArraySegment<byte>> _byteStream;
        readonly Stack<ArrayState> _arrayState;

        ReadState _currentState;
        (ResponseType type, long length, int offset) _currentType;

        ArrayState _currentArrayState;

        public RedisByteSequenceReader(IEnumerable<ArraySegment<byte>> byteStream)
        {
            _byteStream = byteStream;
            _currentState = ReadState.Type;
            _currentType = (ResponseType.Unknown, 0, 0);
            _arrayState = new Stack<ArrayState>();
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

            var level = 0;
            var index = 0;

            if (_currentArrayState != null)
            {
                _currentArrayState.Increment();

                level = _currentArrayState.Level + 1;
                index = _currentArrayState.Index;

                _arrayState.Push(_currentArrayState);
            }

            _currentArrayState = new ArrayState()
            {
                Index = 0,
                Length = length,
                Level = level
            };

            var array = new RedisObjectPart()
            {
                IsArrayStart = true,
                Length = length,
                Level = _currentArrayState.Level,
                ArrayIndex = index
            };

            CompleteArray();

            return array;
        }

        RedisObjectPart YieldObjectPart(RedisObject value)
        {
            _currentState = ReadState.Type;
            
            if (_currentArrayState == null || value == null)
            {
                return new RedisObjectPart
                {
                    Value = value
                };
            }

            _currentArrayState.Increment();

            var item = new RedisObjectPart
            {
                Value = value,
                ArrayIndex = _currentArrayState.Index,
                Level = _currentArrayState.Level,
                Length = _currentArrayState.Length
            };

            CompleteArray();

            return item;
        }

        void CompleteArray()
        {
            while ((_currentArrayState?.IsComplete).GetValueOrDefault())
            {
                if (_arrayState.Count > 0)
                {
                    _currentArrayState = _arrayState.Pop();
                }
                else
                {
                    _currentArrayState = null;
                }
            }
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

        class ArrayState
        {
            public int Level;
            public int Index;
            public long Length;
            public bool IsComplete => Index == Length;

            public void Increment()
            {
                Index++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}