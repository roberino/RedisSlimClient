using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Serialization.CustomSerializers
{
    static class EnumerableSerializerExtensions
    {
        const string ElementName = "$items";

        static readonly ConcurrentDictionary<Type, object> _cache = new ConcurrentDictionary<Type, object>();

        public static IObjectSerializer<T> Create<T>()
        {
            var ct = typeof(T);

            return (IObjectSerializer<T>) _cache.GetOrAdd(ct, t =>
            {
                if (ct.IsArray)
                {
                    var ast = typeof(ArraySerializer<>);

                    var et = ct.GetElementType();

                    var x = ast.MakeGenericType(et);

                    return Activator.CreateInstance(x);
                }

                {
                    var ifc = ct.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

                    var et = ifc.GetGenericArguments()[0];

                    var es = typeof(EnumerableSerializer<,>);

                    var x = es.MakeGenericType(et, ct);

                    return Activator.CreateInstance(x);
                }
            });
        }

        class EnumerableSerializer<TElement, TCollection> : IObjectSerializer<TCollection>
            where TCollection : class, ICollection<TElement>
        {
            public void WriteData(TCollection instance, IObjectWriter writer)
            {
                writer.WriteItem(ElementName, (IEnumerable<TElement>) instance);
            }

            public TCollection ReadData(IObjectReader reader, TCollection defaultValue)
            {
                var items = reader.ReadEnumerable(ElementName, defaultValue);

                return (items as TCollection) ?? (items.ToList() as TCollection);
            }
        }

        class ArraySerializer<TElement> : IObjectSerializer<TElement[]>
        {
            public void WriteData(TElement[] instance, IObjectWriter writer)
            {
                writer.WriteItem(ElementName, (IEnumerable<TElement>) instance);
            }

            public TElement[] ReadData(IObjectReader reader, TElement[] defaultValue)
            {
                var result = reader.ReadEnumerable(ElementName, defaultValue);

                return (result as TElement[]) ?? result.ToArray();
            }
        }
    }
}
