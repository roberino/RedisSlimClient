using System;
using System.Runtime.Serialization;

namespace RedisTribute.Serialization.Objects
{
    class SerializerExceptionDecorator<T> : IObjectSerializer<T>
    {
        static readonly object _lockObj = new object();
        static IObjectSerializer<T> _default;

        readonly IObjectSerializer<T> _serializer;

        public SerializerExceptionDecorator(IObjectSerializer<T> serializer)
        {
            _serializer = serializer;
        }

        public static IObjectSerializer<T> Default(Func<IObjectSerializer<T>> serializerFactory)
        {
            if (_default != null)
            {
                return _default;
            }

            lock (_lockObj)
            {
                if (_default == null)
                {
                    _default = new SerializerExceptionDecorator<T>(serializerFactory());
                }

                return _default;
            }
        }

        public T ReadData(IObjectReader reader, T defaultValue)
        {
            try
            {
                return _serializer.ReadData(reader, defaultValue);
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException("Read error", ex);
            }
        }

        public void WriteData(T instance, IObjectWriter writer)
        {
            try
            {
                _serializer.WriteData(instance, writer);
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException("Write error", ex);
            }
        }
    }
}
