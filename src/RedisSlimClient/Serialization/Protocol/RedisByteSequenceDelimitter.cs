using System;
using System.Buffers;
using System.Text;

namespace RedisSlimClient.Serialization.Protocol
{
    class RedisByteSequenceDelimitter
    {
        const byte _endChar0 = (byte)'\r';
        const byte _endChar1 = (byte)'\n';
        const byte _bulkString = (byte)ResponseType.BulkStringType;

        int _currentPosition;
        int _startBulkString;
        ReadMode _readMode;
        long? _currentReadLength;

        public SequencePosition? Delimit(ReadOnlySequence<byte> sequence)
        {
            if (_readMode == ReadMode.BulkStringContent)
            {
                return GetCurrentPosition(sequence, (int)_currentReadLength.Value + 2);
            }

            foreach (var memory in sequence)
            {
                foreach (var b in memory.Span)
                {
                    _currentPosition++;

                    if (b == _endChar0)
                    {
                        if (_readMode == ReadMode.None)
                        {
                            _readMode = ReadMode.StartMarker;
                            continue;
                        }

                        if (_readMode == ReadMode.BulkStringLength)
                        {
                            return GetCurrentPosition(sequence, 1);
                        }
                    }
                    else
                    {
                        if (b == _endChar1)
                        {
                            if (_readMode == ReadMode.StartMarker)
                            {
                                return GetCurrentPosition(sequence);
                            }
                        }
                        else
                        {
                            if (b == _bulkString && _readMode == ReadMode.None)
                            {
                                _readMode = ReadMode.BulkStringLength;
                                _startBulkString = _currentPosition;
                                continue;
                            }
                        }
                    }
                }
            }

            return null;
        }

        void Reset()
        {
            _currentPosition = 0;
            _startBulkString = 0;
            _currentReadLength = null;
            _readMode = ReadMode.None;
        }

        SequencePosition? GetCurrentPosition(ReadOnlySequence<byte> sequence, int offset = 0)
        {
            var index = _currentPosition + offset - 1;

            if(index >= sequence.Length)
            {
                return default;
            }

            if (_readMode == ReadMode.BulkStringLength)
            {
                _currentReadLength = ParseLength(sequence);
                _readMode = ReadMode.BulkStringContent;
                _startBulkString = 0;
                _currentPosition = 0;
            }
            else
            {
                Reset();
            }

            return sequence.GetPosition(index);
        }

        long ParseLength(ReadOnlySequence<byte> sequence)
        {
            var seq = sequence.Slice(_startBulkString, _currentPosition - _startBulkString - 1);
            var txt = Encoding.ASCII.GetString(seq.ToArray());

            return long.Parse(txt);
        }

        enum ReadMode : byte
        {
            None,
            StartMarker,
            BulkStringLength,
            BulkStringContent
        }
    }
}
