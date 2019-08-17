using System;

namespace RedisSlimClient.Configuration
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
                if (value == null)
                {
                    throw new ArgumentNullException(typeof(T).Name);
                }

                _value = value;
            }
        }


        public static implicit operator NonNullable<T>(T x) => new NonNullable<T>(x);

        public static implicit operator T (NonNullable<T> x) => x.Value;
    }
}