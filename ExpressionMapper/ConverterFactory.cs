using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

using ExpressionMapper.Conversion;
using ExpressionMapper.Extensions;

namespace ExpressionMapper
{
    public class ConverterFactory
    {
        private static readonly MethodInfo _default;
        private static readonly MethodInfo _collectionDefault;
        private static readonly Type _object = typeof(Object);
        private static readonly ConcurrentDictionary<String, MethodInfo> _customConverters = new ConcurrentDictionary<String, MethodInfo>();
        private static readonly String _keyFormat = "{0},{1}";

        static ConverterFactory()
        {
            // load default converter
            _default = typeof(ConversionHelper)
                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(mi =>
                            {
                                if (mi.Name == "Convert")
                                {
                                    var parms = mi.GetParameters();

                                    if (parms.Length == 2
                                        && parms[0].ParameterType == _object
                                        && parms[1].ParameterType.IsGenericParameter)
                                    {
                                        return true;
                                    }
                                }

                                return false;
                            })
                            .First();

            _collectionDefault = typeof(ConversionHelper)
                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(mi =>
                            {
                                if (mi.Name == "ConvertCollection")
                                {
                                    var parms = mi.GetParameters();

                                    if (parms.Length == 2)
                                    {
                                        return true;
                                    }
                                }

                                return false;
                            })
                            .First();
        }

        private BinaryExpression CreateInstanceAndAssign(Expression target)
        {
            var ctor = Expression.New(target.Type);
            var instance = Expression.MemberInit(ctor);
            var assignment = Expression.Assign(target, instance);

            return assignment;
        }

        private static String GetKey(Type from, Type to)
        {
            return String.Format(_keyFormat, from.FullName, to.FullName);
        }

        private Expression GetConversionExpression(MemberExpression get, MemberExpression set)
        {
            var conversion = _default.MakeGenericMethod(set.Type);
            var convertVal = GetReferenceExpression(get);
            var defaultVal = Expression.Default(set.Type);

            return Expression.Call(conversion, convertVal, defaultVal);
        }

        private static Expression GetConversionExpression(MethodInfo conversion, MemberExpression get)
        {
            return Expression.Call(conversion, get);
        }

        /// <summary>
        /// box if get type is value type
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        private static Expression GetReferenceExpression(Expression get)
        {
            if (get.Type.IsValueType)
            {
                return Expression.Convert(get, _object);
            }

            return get;
        }

        /// <summary>
        /// Register custom conversion function
        /// </summary>
        /// <typeparam name="From"></typeparam>
        /// <typeparam name="To"></typeparam>
        /// <param name="custom"><see cref="Func<From, To>"/></param>
        /// <returns>ConverterFactory for chaining</returns>
        public ConverterFactory RegisterCustomConversionExpression<From, To>(Func<From, To> custom)
        {
            if (custom == null)
            {
                throw new ArgumentNullException("Custom Converter must not be null");
            }

            var key = GetKey(typeof(From), typeof(To));

            _customConverters.TryAdd(key, custom.Method);

            return this;
        }

        /// <summary>
        /// Get a conversion function for a Member of Type [From] to a Member of Type [To]
        /// </summary>
        /// <param name="get"></param>
        /// <param name="set"></param>
        /// <returns>Conversion Expression</returns>
        public Expression GetConverter(MemberExpression get, MemberExpression set)
        {
            var key = GetKey(get.Type, set.Type);

            if (_customConverters.ContainsKey(key))
            {
                return GetConversionExpression(_customConverters[key], get);
            }

            return GetDefaultConverter(get, set);
        }

        private Expression GetDefaultConverter(MemberExpression get, MemberExpression set) 
        {
            // check if this is a Collection type
            if (AreCollections(get, set))
            {
                if (get.Type.IsGenericIEnumerable()
                    && set.Type.IsGenericIEnumerable())
                {
                    var fromTypeArgs = get.Type.GetGenericArguments();
                    var toTypeArgs = set.Type.GetGenericArguments();

                    if (fromTypeArgs.Count() == fromTypeArgs.Count())
                    {
                        if (fromTypeArgs.Count() > 2)
                        {
                            throw new NotSupportedException("Only simple collection types are supported, you may be able to use CustomConverter!");
                        }

                        if (fromTypeArgs.Count() == 1)
                        {
                            return ConvertCollection(get, set, fromTypeArgs, toTypeArgs);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Only simple compatible collection types are supported, you may be able to use CustomConverter!");
                    }

                }

                return null;
            }
            else
            {
                return GetConversionExpression(get, set);
            }
        }

        private Expression ConvertCollection(MemberExpression get, MemberExpression set, Type[] fromTypeArgs, Type[] toTypeArgs)
        {
            var convertCollection = _collectionDefault.MakeGenericMethod(fromTypeArgs[0], toTypeArgs[0]);
            var toInstantiation = CreateInstanceAndAssign(set);

            var fromArg = Expression.Convert(get, convertCollection.GetParameters()[0].ParameterType);
            var toArg = Expression.Convert(set, convertCollection.GetParameters()[1].ParameterType);

            var convert = Expression.Call(convertCollection, fromArg, toArg);

            return Expression.Block(set.Type, toInstantiation, convert, set);
        }

        private static readonly Type _string = typeof(String);

        private static bool AreCollections(MemberExpression get, MemberExpression set)
        {
            return get.Type.IsIEnumerable()
                && set.Type.IsIEnumerable()
                && get.Type != _string
                && set.Type != _string;
        }
    }
}
