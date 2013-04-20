using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;

namespace ExpressionMapper.Conversion
{
    public static class ConversionHelper
    {
        private static readonly Type _convertible = typeof(IConvertible);
        private static readonly String _nullRefError = "Collection of Type {0} cannot be null";

        /// <summary>
        /// Converts a value of unknown type to a value typeof(T)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Convert<T>(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Value must not be null");
            }

            return ConvertType<T>(value);
        }

        /// <summary>
        /// Converts a value of unknown type to a value typeof(T).
        /// If value is null the a default is returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Convert<T>(Object value, T defaultValue)
        {
            if (value != null)
            {
                try
                {
                    return ConvertType<T>(value);
                }
                catch (Exception ex)
                {
                    var error = ex;
                    /*
                     * Base core library will not have any dependencies including logging
                     * we intentionally swallow up the exception since we can return defaul
                     */
                }
            }

            return defaultValue;
        }

        private static T ConvertType<T>(Object value)
        {
            // quick convert if value is type T
            if (value is T)
            {
                return (T)value;
            }

            var origin = value.GetType();
            var destination = GetConvertType<T>();

            if (_convertible.IsAssignableFrom(origin)
                && _convertible.IsAssignableFrom(destination))
            {
                // see if the type implements IConvertible which is cheaper operation
                return (T)System.Convert
                                .ChangeType(value, destination);
            }
            else
            {
                return Convert<T>(value, origin, destination);
            }
        }

        /// <summary>
        /// Brute force convert, works with Guids and other sundry types
        /// and custom types that implement IComponent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        private static T Convert<T>(Object value, Type origin, Type destination)
        {
            var destinationConverter = TypeDescriptor.GetConverter(destination);

            if (destinationConverter.CanConvertFrom(origin))
            {
                // Converted from origin type from origin type 
                return (T)destinationConverter.ConvertFrom(value);
            }

            // else try to convert to destination using an orgin type converter
            return (T)TypeDescriptor.GetConverter(origin)
                                    .ConvertTo(value, destination);
        }

        /// <summary>
        /// Determine if a type is nullable. Nullable types will 
        /// break the standard ChangeType function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static Type GetConvertType<T>()
        {
            // Check if nullable type. Strings are peculiar since they are nullable
            // but dont have an underlying type
            if (default(T) == null && typeof(T) != typeof(string))
            {
                return Nullable.GetUnderlyingType(typeof(T));
            }
            else
                return typeof(T);
        }

        public static void ConvertCollection<From, To>(IEnumerable<From> fromCollection, ICollection<To> toCollection)
        {
            var fromType = typeof(From);

            if (fromCollection == null
                || fromCollection.Count() < 1)
            {
                return;
            }

            if (toCollection == null)
            {
                throw new ArgumentNullException(_nullRefError, typeof(To).FullName);
            }

            foreach (var from in fromCollection.Where((f) => !fromType.IsValueType && f != null ))
            {
                toCollection.Add(Convert<To>(from));
            }
        }
    }
}
