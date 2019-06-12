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
                            _currentReadLength = ParseLength(sequence);
                            _readMode = ReadMode.BulkStringContent;
                            _startBulkString = _currentPosition + 1;
                            continue;
                        }
                    }
                    else
                    {
                        if (b == _endChar1)
                        {
                            if (_readMode == ReadMode.StartMarker)
                            {
                                _readMode = ReadMode.EndMarker;
                                continue;
                            }

                            if (_currentReadLength.HasValue && _currentPosition > (_startBulkString + _currentReadLength.Value))
                            {
                                return GetCurrentPosition(sequence);
                            }
                        }
                        else
                        {
                            if (b == _bulkString && (_readMode == ReadMode.EndMarker || _readMode == ReadMode.None))
                            {
                                _readMode = ReadMode.BulkStringLength;
                                _startBulkString = _currentPosition;
                                continue;
                            }
                        }
                    }

                    if (_readMode == ReadMode.EndMarker)
                    {
                        return GetCurrentPosition(sequence, -1);
                    }
                }
            }

            if (_readMode == ReadMode.EndMarker)
            {
                return GetCurrentPosition(sequence);
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

        SequencePosition GetCurrentPosition(ReadOnlySequence<byte> sequence, int offset = 0)
        {
            var pos = _currentPosition;

            Reset();

            return sequence.GetPosition(pos + offset);
        }

        long ParseLength(ReadOnlySequence<byte> sequence)
        {
            var seq = sequence.Slice(_startBulkString, _currentPosition - _startBulkString);

            return long.Parse(Encoding.ASCII.GetString(seq.ToArray()));
        }

        enum ReadMode : byte
        {
            None,
            StartMarker,
            EndMarker,
            BulkStringLength,
            BulkStringContent
        }
    }
}
