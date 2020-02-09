using System.Reflection.Emit;

namespace RedisTribute.Serialization.Emit
{
    static class ClassBuilderStrategy
    {
        public static void Build<T>(this TypeBuilder newAccessorType)
        {
            var targetType = typeof(T);

            if (targetType.IsValueTuple())
            {
                var targetFields = targetType.SerializableFields();

                new WriteObjectImplBuilder<T>(newAccessorType, targetFields).Build();

                new ReadObjectImplBuilder<T>(newAccessorType, targetFields, true).Build();
            }
            else
            {
                var targetProps = targetType.SerializableProperties();

                new WriteObjectImplBuilder<T>(newAccessorType, targetProps).Build();

                new ReadObjectImplBuilder<T>(newAccessorType, targetProps).Build();
            }
        }
    }
}
