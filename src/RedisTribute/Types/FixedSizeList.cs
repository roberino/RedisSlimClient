using System;

namespace RedisTribute.Types
{
    struct FixedSizeList<T>
    {
        T[] _elements;
        int _size;
        int _pos;

        public void IncreaseSize(int numberOfNewElements)
        {
            _size += numberOfNewElements;

            if (_elements != null)
            {
                throw new InvalidOperationException();
            }
        }
        public void IncreaseSizeIf(int numberOfNewElements, bool condition)
        {
            if (!condition) { return; }

            _size += numberOfNewElements;

            if (_elements != null)
                _elements = null;
        }

        public void Add(T item)
        {
            GetBuffer()[_pos++] = item;
        }

        public T[] GetBuffer()
        {
            if (_elements == null)
            {
                _elements = new T[_size];
            }

            return _elements;
        }
    }
}
