using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

using ExpressionMapper.Conversion;
using ExpressionMapper.Extensions;

namespace ExpressionMapper.Mapping
{
    public delegate void ImplicitMap<From, To>();

    /// <summary>
    /// Maps Object properties and fields to another Object based on property field names
    /// </summary>
    /// <remarks>
    /// Creates a straight mapping for Reference types
    /// </remarks>
    public class ExpressionMapperFactory
    {
        private static readonly String _fromNullRefExMessage = "The ({0}) From parameter Object reference cannot be null!";
        private static readonly String _nullRefExMessage = "The ({0}) From parameter and ({1}) To parameter Object references cannot be null!";
        private static readonly String _collectionMappingError = "Unable to map {0} to {1} Only simple collection implementing ICollection or ICollection<KeyValuePair<>>";

        private static readonly BindingFlags _flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance;

        private static readonly ConstantExpression _nullRef = Expression.Constant(null);

        private static readonly ConverterFactory _converter = new ConverterFactory();

        /// <summary>
        /// Create instance of To Type
        /// </summary>
        /// <param name="toType"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private BinaryExpression CreateInstanceAndAssign(Expression target)
        {
            var ctor = Expression.New(target.Type);
            var instance = Expression.MemberInit(ctor);
            var assignment = Expression.Assign(target, instance);

            return assignment;
        }

        /// <summary>
        /// Create an expression that represents throwing a Null Reference exception with the given error message
        /// </summary>
        /// <param name="fromType"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private UnaryExpression CreateNullRefException(String errorMessage)
        {
            var throwNullFromEx = Expression.Constant(new NullReferenceException(errorMessage));
            var throwEx = Expression.Throw(throwNullFromEx);
            return throwEx;
        }

        /// <summary>
        /// Creates a Null reference Expression testing both the From instance and To instance
        /// </summary>
        /// <param name="fromParameter"></param>
        /// <param name="toParameter"></param>
        /// <returns></returns>
        private BinaryExpression CreateNullParameterCheck(ParameterExpression fromParameter, ParameterExpression toParameter)
        {
            var fromParameterNullCheck = Expression.Equal(fromParameter, _nullRef);
            var toParameterNullCheck = Expression.Equal(toParameter, _nullRef);

            return Expression.OrElse(fromParameterNullCheck, toParameterNullCheck);
        }

        /// <summary>
        /// Create field mappings between From and To Objects
        /// </summary>
        /// <param name="fromType"></param>
        /// <param name="toType"></param>
        /// <param name="fromParameter"></param>
        /// <param name="toParameter"></param>
        /// <returns></returns>
        private IEnumerable<Expression> CreateFieldMappings(Expression fromParameter, Expression toParameter)
        {
            var fieldMappings = new List<Expression>();

            foreach (var from in fromParameter.Type.GetFields(_flags))
            {
                foreach (var to in toParameter.Type.GetFields(_flags))
                {
                    if (String.Compare(from.Name, to.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        var get = Expression.PropertyOrField(fromParameter, from.Name);
                        var set = Expression.PropertyOrField(toParameter, to.Name);

                        if (!from.FieldType.IsValueType)
                        {
                            var assign = SetReferenceMapping(get, set);

                            fieldMappings.Add(assign);
                        }
                        else
                        {
                            var fromIsNullable = from.FieldType.IsNullable();
                            var toIsNullable = to.FieldType.IsNullable();

                            if (fromIsNullable == toIsNullable)
                            {
                                var assign = GetMatchingNullableMapping(from.FieldType, to.FieldType, get, set);

                                fieldMappings.Add(assign);
                            }
                            else
                            {
                                var assign = GetMismatchedNullableMapping(get, set, fromIsNullable);

                                fieldMappings.Add(assign);
                            }
                        }

                        // cause there can only be one
                        break;
                    }
                }
            }

            return fieldMappings;
        }

        /// <summary>
        /// Create Property mappings between From and To Objects
        /// </summary>
        /// <param name="fromType"></param>
        /// <param name="toType"></param>
        /// <param name="fromParameter"></param>
        /// <param name="toParameter"></param>
        /// <returns></returns>
        private IEnumerable<Expression> CreatePropertyMappings(Expression fromParameter, Expression toParameter)
        {
            var propertyMappings = new List<Expression>();

            foreach (var from in fromParameter.Type.GetProperties(_flags))
            {
                foreach (var to in toParameter.Type.GetProperties(_flags))
                {
                    if (String.Compare(from.Name, to.Name, StringComparison.InvariantCultureIgnoreCase) == 0
                        && from.CanRead
                        && to.CanWrite)
                    {
                        var get = Expression.PropertyOrField(fromParameter, from.Name);
                        var set = Expression.PropertyOrField(toParameter, to.Name);

                        if (!from.PropertyType.IsValueType)
                        {
                            var assign = SetReferenceMapping(get, set);

                            propertyMappings.Add(assign);
                        }
                        else
                        {
                            var fromIsNullable = from.PropertyType.IsNullable();
                            var toIsNullable = to.PropertyType.IsNullable();

                            if (fromIsNullable == toIsNullable)
                            {
                                var assign = GetMatchingNullableMapping(from.PropertyType, to.PropertyType, get, set);

                                propertyMappings.Add(assign);
                            }
                            else
                            {  
                                var assign = GetMismatchedNullableMapping(get, set, fromIsNullable);

                                propertyMappings.Add(assign);
                            }
                        }

                        // cause there can only be one
                        break;
                    }
                }
            }

            return propertyMappings;
        }

        /// <summary>
        /// Create mapping between a Property/Field where the From member is Nullable or ValueType and the To member is not
        /// </summary>
        /// <param name="to"></param>
        /// <param name="get"></param>
        /// <param name="set"></param>
        /// <param name="fromIsNullable"></param>
        /// <returns></returns>
        private Expression GetMismatchedNullableMapping(MemberExpression get, MemberExpression set, bool fromIsNullable)
        {
            // Target types match
            var typesMatch = fromIsNullable ? (Nullable.GetUnderlyingType(get.Type) == set.Type) : (get.Type == Nullable.GetUnderlyingType(set.Type));

            if (typesMatch)
            {
                // simple conversion from T to Nullable<T> and vice versa
                var conversion = Expression.Convert(get, set.Type);
                var assign = Expression.Assign(set, conversion);

                // if from is nullable we have to set the default value for a ValueType in
                // the case of a null
                if (fromIsNullable)
                {
                    var nullCheck = Expression.NotEqual(get, _nullRef);
                    var valDefault = Expression.Default(set.Type);
                    var assignDefault = Expression.Assign(set, valDefault);

                    return Expression.IfThenElse(nullCheck, assign, assignDefault);
                }

                return assign;
            }
            else
            {
                var getter = _converter.GetConverter(get, set);

                return Expression.Assign(set, getter);
            }
        }

        /// <summary>
        /// Create mapping between a Property/Field where the From member is a ValueType or Nullable and the To member is also ValueType or Nullable, but not
        /// necessarily the same type
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="get"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        private BinaryExpression GetMatchingNullableMapping(Type from, Type to, MemberExpression get, MemberExpression set)
        {
            Expression getter = get;

            // if these types are mismatched call convert helper
            if (from != to)
            {
                getter = _converter.GetConverter(get, set);
            }

            return Expression.Assign(set, getter);
        }

        /// <summary>
        /// Create mapping between Property/Field where the From member is a reference type
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="get"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        private Expression SetReferenceMapping(MemberExpression get, MemberExpression set)
        {
            Expression getter = get;

            // if these types are mismatched call convert helper
            if (get.Type != set.Type)
            {
                getter = _converter.GetConverter(get, set);
            }

            return Expression.Assign(set, getter);
        }

        /// <summary>
        /// Paramater instance null check assertion
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private ConditionalExpression CreateParameterAssertion(ParameterExpression from, ParameterExpression to)
        {
            var assertInstances = Expression.IfThen(CreateNullParameterCheck(from, to),
                                                    CreateNullRefException(String.Format(_nullRefExMessage, from.Type.FullName, to.Type.FullName)));
            return assertInstances;
        }

        /// <summary>
        /// Paramater instance null check assertion
        /// </summary>
        /// <param name="fromParameter"></param>
        /// <returns></returns>
        private ConditionalExpression CreateParameterAssertion(ParameterExpression fromParameter)
        {
            var nullRefCheck =  Expression.Equal(fromParameter, _nullRef);
            
            var assertFromInstance = Expression.IfThen(nullRefCheck,
                                                    CreateNullRefException(String.Format(_fromNullRefExMessage, fromParameter.Type.FullName)));
            return assertFromInstance;
        }

        public ExpressionMapperFactory RegisterCustomConverter<From, To>(Func<From, To> converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException("Custom Converter cannot be null!");
            }
   
            _converter.RegisterCustomConversionExpression(converter);

            return this;
        }
        /// <summary>
        /// Creates a compiled lambda expression thats maps public properties and fields from one Object
        /// to another
        /// </summary>
        /// <typeparam name="From">Object to map from</typeparam>
        /// <typeparam name="To">Object to map to</typeparam>
        /// <returns>Mapper delegate of type Action&lt;From, To&gt;</returns>
        public Action<From, To> CreateImplicitTypeMapper<From,To>()
        {
            // declare parameter expressions
            var fromParameter = Expression.Parameter(typeof(From), "From");
            var toParameter = Expression.Parameter(typeof(To), "To");

            // assert parameter instances
            var assertInstances = CreateParameterAssertion(fromParameter, toParameter);

            // create collection of expression to be processed
            var expression = new List<Expression> { 
                assertInstances 
            };

            // get property and field mappings
            var propertyMappings = CreatePropertyMappings(fromParameter, toParameter);
            var fieldMappings = CreateFieldMappings(fromParameter, toParameter);

            expression.AddRange(propertyMappings);
            expression.AddRange(fieldMappings);

            return Expression.Lambda<Action<From, To>>(
                                Expression.Block(new List<ParameterExpression> { },
                                    expression),
                                fromParameter,
                                toParameter
                                )
                                .Compile();
        }

        /// <summary>
        /// Creates a compiled lambda expression that creates a new instance of Type To, and implicitly maps 
        /// Properties/Fields from instance of type From
        /// </summary>
        /// <typeparam name="From"></typeparam>
        /// <typeparam name="To"></typeparam>
        /// <returns></returns>
        public Func<From, To> CreateImplicitlyMappedType<From, To>() where To : new()
        {
            // declare parameter expressions
            var fromParameter = Expression.Parameter(typeof(From), "from");
            var toParameter = Expression.Variable(typeof(To), "to");

            // assert From instance is not null
            var assertFromInstance = CreateParameterAssertion(fromParameter);

            // instantiate new To object to map and return
            var toInstantiation = CreateInstanceAndAssign(toParameter);

            // create expressions collection
            var expressions = new List<Expression> { 
                assertFromInstance, 
                toInstantiation 
            };

            var propertyMappingExpressions = CreatePropertyMappings(fromParameter, toParameter);
            var fieldMappingExpressions = CreateFieldMappings(fromParameter, toParameter);

            expressions.AddRange(propertyMappingExpressions);
            expressions.AddRange(fieldMappingExpressions);

            // add the To instance expression last which means it is the return value of the expression block
            expressions.Add(toParameter);

            return Expression.Lambda<Func<From, To>>(
                                    Expression.Block(toParameter.Type,
                                        new ParameterExpression[] { toParameter },
                                        expressions
                                    ),
                                    fromParameter
                                ).Compile();
        }
    }
}
