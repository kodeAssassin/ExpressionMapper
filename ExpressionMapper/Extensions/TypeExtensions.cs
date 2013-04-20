using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ExpressionMapper.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Type _nullable = typeof(Nullable<>);
        private static readonly Type _genericCollection = typeof(ICollection<>);
        private static readonly Type _iEnumerable = typeof(IEnumerable);
        private static readonly Type _genericIEnumerable = typeof(IEnumerable<>);
        private static readonly Type _genericKeyValuePair = typeof(KeyValuePair<,>);

        /// <summary>
        /// Determines whether the <paramref name="genericType"/> is assignable from
        /// <paramref name="type"/> taking into account generic definitions
        /// </summary>
        private static bool Implements(this Type type, Type implemented)
        {
            return type.IsGenericType
                && (type == implemented
                    || type.IsGenericTypeDefinition(implemented)
                    || type.TypeImplementsDefinition(implemented)
                    || type.Implements(implemented));
        }

        private static bool TypeImplementsDefinition(this Type type, Type implemented)
        {
            return type.GetInterfaces()
                        .Where(it => it.IsGenericType)
                        .Any(it => it.GetGenericTypeDefinition() == implemented);
        }

        private static bool IsGenericTypeDefinition(this Type type, Type implemented)
        {
            return implemented.IsGenericTypeDefinition
                    && type.IsGenericType
                    && type.GetGenericTypeDefinition() == implemented;
        }

        /// <summary>
        /// Tests whether the referenced type is Nullable
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>True if the type is nullable, otherwise false</returns>
        public static bool IsNullable(this Type type) 
        {
            return type.IsGenericType &&
                    type.GetGenericTypeDefinition() == _nullable;
        }

        public static bool IsGenericIEnumerable(this Type type)
        {
            return type.Implements(_genericIEnumerable);
        }

        public static bool IsGenericCollection(this Type type)
        {
            return type.Implements(_genericCollection);
        }

        public static bool IsIEnumerable(this Type type)
        {
            return _iEnumerable.IsAssignableFrom(type);
        }

        public static bool IsGenericKeyValuePair(this Type type)
        {
            return type.IsGenericType
                && _genericKeyValuePair.IsAssignableFrom(type);
        }
    }
}
