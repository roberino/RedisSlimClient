﻿using System;
using System.Buffers;

namespace RedisTribute.Types.Primatives
{
    readonly struct MemoryByteSequenceAdapter : IByteSequence
    {
        readonly ReadOnlySequence<byte> _sequence;

        public MemoryByteSequenceAdapter(ReadOnlySequence<byte> sequence)
        {
            _sequence = sequence;
        }

        public int Length => (int)_sequence.Length;

        public void CopyTo(byte[] array)
        {
            _sequence.CopyTo(new Span<byte>(array));
        }

        public byte[] ToArray(int offset) =>
            _sequence.Slice(offset).ToArray();

        public byte GetValue(int index) =>
            _sequence.IsSingleSegment ? _sequence.First.Span[index] : _sequence.Slice(index, 1).ToArray()[0];

        public ReadOnlySequence<byte> ToSequence(int offset) => _sequence.Slice(offset);
    }
}