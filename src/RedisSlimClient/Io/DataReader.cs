using RedisSlimClient.Io.Types;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RedisSlimClient.Io
{
    class DataReader : IEnumerable<RedisObject>, IDisposable
    {
        readonly IEnumerable<ArraySegment<byte>> _byteStream;

        ReadState _currentState;
        (ResponseType type, long length, int offset) _currentType;
        RedisArray _currentArray;

        public DataReader(IEnumerable<ArraySegment<byte>> byteStream)
        {
            _byteStream = byteStream;
            _currentState = ReadState.Type;
            _currentType = (ResponseType.Unknown, 0, 0);
        }

        public IEnumerator<RedisObject> GetEnumerator()
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
                            var value = YieldObject(new RedisInteger(_currentType.length));

                            if (value != null)
                            {
                                yield return value;
                            }

                            continue;
                        }
                    }
                }

                {
                    var value = YieldObject(segment);

                    if (value != null)
                    {
                        yield return value;
                    }
                }
            }
        }

        void OpenArray(long length)
        {
            _currentState = ReadState.Type;
            _currentArray = new RedisArray(length);
        }

        RedisObject YieldObject(RedisObject value)
        {
            if (_currentArray == null)
            {
                return value;
            }

            _currentArray.Items.Add(value);

            if (_currentArray.IsComplete)
            {
                value = _currentArray;

                _currentArray = null;

                return value;
            }

            return null;
        }

        RedisObject YieldObject(ArraySegment<byte> segment)
        {
            var value =
                _currentType.type == ResponseType.ErrorType
                    ? (RedisObject)new RedisError(segment.ToAsciiString(_currentType.offset))
                    : new RedisString(segment.ToBytes(_currentType.offset));

            return YieldObject(value);
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