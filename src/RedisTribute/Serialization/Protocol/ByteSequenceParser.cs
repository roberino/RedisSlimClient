using RedisTribute.Types;
using RedisTribute.Types.Primatives;
using System;
using System.Collections.Generic;

namespace RedisTribute.Serialization
{
    class ByteSequenceParser
    {
        readonly Stack<ArrayState> _arrayState;

        ReadState _currentState;
        (ResponseType type, long length, int offset) _currentType;

        ArrayState? _currentArrayState;

        public ByteSequenceParser()
        {
            _currentState = ReadState.Type;
            _currentType = (ResponseType.Unknown, 0, 0);
            _arrayState = new Stack<ArrayState>();
        }

        public IEnumerable<RedisObjectPart> ReadItem(IByteSequence segment)
        {
            if (_currentState == ReadState.Type)
            {
                _currentType = segment.ToResponseType();

                switch (_currentType.type)
                {
                    case ResponseType.BulkStringType:
                        if (_currentType.length < 0)
                        {
                            var part = YieldObjectPart(RedisNull.Value);

                            if (!part.IsEmpty)
                            {
                                yield return part;
                            }

                            yield break;
                        }

                        _currentState = ReadState.Value;
                        yield break;
                    case ResponseType.ArrayType:
                        yield return OpenArray(_currentType.length);
                        yield break;
                    case ResponseType.IntType:
                        {
                            var part = YieldObjectPart(new RedisInteger(_currentType.length));

                            if (!part.IsEmpty)
                            {
                                yield return part;
                            }

                            yield break;
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

        RedisObjectPart YieldObjectPart(IRedisObject value)
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

        IRedisObject GetCurrentValue(IByteSequence segment)
        {
            switch (_currentType.type)
            {
                case ResponseType.ErrorType:
                    return new RedisError(segment.ToAsciiString(_currentType.offset));
                case ResponseType.StringType:
                    return new RedisString(segment.ToSequence(_currentType.offset));
                case ResponseType.BulkStringType:
                    return new RedisString(segment.ToSequence(_currentType.offset));
            }

            throw new NotSupportedException(_currentType.type.ToString());
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
    }
}
