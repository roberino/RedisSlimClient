using System;

namespace RedisTribute.Configuration
{
    sealed class NonNullable<T> where T  : class
    {
        T _value;

        public NonNullable(T defaultValue)
        {
            Value = defaultValue;
        }

        public T Value
        {
            get => _value;
            set
            {
                _value = value ?? throw new ArgumentNullException(typeof(T).Name);
            }
        }


        public static implicit operator NonNullable<T>(T x) => new NonNullable<T>(x);

        public static implicit operator T (NonNullable<T> x) => x.Value;
    }
}