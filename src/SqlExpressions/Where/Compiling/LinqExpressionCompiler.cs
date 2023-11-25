using System.Reflection;
using SqlExpressions.Where.Ast;
using Linq = System.Linq.Expressions;

namespace SqlExpressions.Where.Compiling;

public class LinqExpressionCompiler
{
    private Type type = null!;
    private PropertyInfo[] properties = null!;
    private Linq.ParameterExpression typeParameter = null!;

    public Linq.Expression<Func<T, bool>> Compile<T>(Expression expression)
    {
        type = typeof(T);
        properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase |
                                        BindingFlags.GetProperty);
        typeParameter = Linq.Expression.Parameter(type);
        var compiled = Compile(expression);
        if (compiled.CanReduce)
        {
            compiled = compiled.ReduceAndCheck();
        }

        return Linq.Expression.Lambda<Func<T, bool>>(compiled, typeParameter);
    }

    private Linq.Expression Compile(Expression expression)
    {
        return expression switch
        {
            CallExpression call => CompileCallExpression(call),
            _ => throw new ArgumentException($"Unexpected Expression of type {expression.GetType().FullName}")
        };
    }

    private Linq.Expression CompileCallExpression(CallExpression call)
    {
        switch (call.OperatorType)
        {
            case OperatorType.Equal:
            case OperatorType.NotEqual:
            case OperatorType.GreaterThan:
            case OperatorType.GreaterThanOrEqual:
            case OperatorType.LessThan:
            case OperatorType.LessThanOrEqual:
                return CompileBinaryExpression(call, call.Operands[0], call.Operands[1]);
            case OperatorType.IsNull:
                return CompileIsNullExpression(call.Operands[0]);
            case OperatorType.IsNotNull:
                return CompileIsNotNullExpression(call.Operands[0]);
            case OperatorType.Like:
                return CompileLikeExpression(call.Operands[0], call.Operands[1]);
            case OperatorType.NotLike:
                return CompileNotLikeExpression(call.Operands[0], call.Operands[1]);
            default:
                throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}");
        }

        return call.OperatorType switch
        {
            // OperatorType.And => CompileAndExpression(call.Operands),
            // OperatorType.Or => CompileOrExpression(call.Operands),
            // OperatorType.In => CompileInExpression(call.Operands[0], call.Operands[1]),
            // OperatorType.NotIn => CompileNotInExpression(call.Operands[0], call.Operands[1]),
            _ => throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}")
        };
    }
    
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", new[] {typeof(string)})!;

    private static readonly MethodInfo StartsWithMethod =
        typeof(string).GetMethod("StartsWith", new[] {typeof(string)})!;

    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod("EndsWith", new[] {typeof(string)})!;

    private Linq.Expression CompileLikeExpression(Expression left, Expression right)
    {
        if (left is PropertyExpression propertyExpression && right is ConstantExpression constantExpression)
        {
            var prop = GetProperty(propertyExpression.PropertyName);
            if (prop.PropertyType != typeof(string))
            {
                throw new ArgumentException(
                    $"Unable to compile Like Expression. PropertyExpression must be a string type but was {prop.PropertyType.Name}");
            }

            if (constantExpression.Value is ConstantValue constantValue &&
                constantValue.ValueType == ConstantValueType.String)
            {
                var value = constantValue.Value.ToString();

                if (value == "")
                {
                    return Linq.Expression.Equal(CompilePropertyExpression(propertyExpression, prop),
                        Linq.Expression.Constant(""));
                }

                if (value == "%")
                {
                    return Linq.Expression.Constant(true); // it is going to match everything.
                }

                var start = value.StartsWith('%');
                var end = value.EndsWith('%');
                if (start && end)
                {
                    return Linq.Expression.Call(
                        CompilePropertyExpression(propertyExpression, prop),
                        ContainsMethod,
                        Linq.Expression.Constant(value.Substring(1, value.Length - 2)));
                }

                if (start)
                {
                    return Linq.Expression.Call(
                        CompilePropertyExpression(propertyExpression, prop),
                        EndsWithMethod,
                        Linq.Expression.Constant(value.Substring(1, value.Length - 1)));
                }

                if (end)
                {
                    return Linq.Expression.Call(
                        CompilePropertyExpression(propertyExpression, prop),
                        StartsWithMethod,
                        Linq.Expression.Constant(value.Substring(0, value.Length - 1)));
                }

                return Linq.Expression.Equal(CompilePropertyExpression(propertyExpression, prop),
                    Linq.Expression.Constant(value));
            }

            throw new ArgumentException(
                $"Unable to compile Like Expression. ConstantExpression must be a string value but was {constantExpression.Value.GetType().Name}");
        }

        throw new ArgumentException(
            $"Unable to compile Like Expression.  Expected left to be PropertyExpression and right to be ConstantExpression but was {left.GetType().Name} and {right.GetType().Name}");
    }

    private Linq.Expression CompileNotLikeExpression(Expression left, Expression right)
    {
        return Linq.Expression.Not(CompileLikeExpression(left, right));
    }

    private Linq.Expression CompileIsNullExpression(Expression identifier)
    {
        if (identifier is PropertyExpression expression)
        {
            var prop = GetProperty(expression.PropertyName);
            if (prop.PropertyType == typeof(string) || Nullable.GetUnderlyingType(prop.PropertyType) != null)
            {
                return Linq.Expression.Equal(CompilePropertyExpression(expression, prop),
                    Linq.Expression.Constant(null, prop.PropertyType));
            }

            return Linq.Expression.Constant(false); // expression is false as Property Type is not null.
        }

        throw new ArgumentException(
            $"Unable to compile Is Null Expression.  Expected operand to be PropertyExpression but is {identifier.GetType().Name}");
    }

    private Linq.Expression CompileIsNotNullExpression(Expression identifier)
    {
        if (identifier is PropertyExpression expression)
        {
            var prop = GetProperty(expression.PropertyName);
            if (prop.PropertyType == typeof(string) || Nullable.GetUnderlyingType(prop.PropertyType) != null)
            {
                return Linq.Expression.NotEqual(CompilePropertyExpression(expression, prop),
                    Linq.Expression.Constant(null, prop.PropertyType));
            }

            return Linq.Expression.Constant(true); // expression will always be true as Property Type is not nullable.
        }

        throw new ArgumentException(
            $"Unable to compile Is Null Expression.  Expected operand to be PropertyExpression but is {identifier.GetType().Name}");
    }

    private Linq.Expression CompileBinaryExpression(CallExpression call, Expression left, Expression right)
    {
        // try and use the type of the PropertyExpression to case the ConstantExpression to.  Also
        // we can validate that the property in the PropertyExpression actually exists on `type`
        return (left, right) switch
        {
            (PropertyExpression leftExpression, ConstantExpression rightExpression) => CompileBinaryConstantExpression(
                call.OperatorType, leftExpression, rightExpression),
            (ConstantExpression leftExpression, PropertyExpression rightExpression) => CompileBinaryConstantExpression(
                call.OperatorType, rightExpression, leftExpression),
            (PropertyExpression leftExpression, PropertyExpression rightExpression) => GenerateBinaryLinqExpression(
                call.OperatorType, CompilePropertyExpression(leftExpression),
                CompilePropertyExpression(rightExpression)),
            (ConstantExpression leftExpression, ConstantExpression rightExpression) => GenerateBinaryLinqExpression(
                call.OperatorType, CompileConstantExpression(leftExpression),
                CompileConstantExpression(rightExpression)),
            _ => throw new ArgumentException(
                "Expected left or right to be either PropertyExpression or ConstantExpression")
        };
    }

    private Linq.Expression CompileBinaryConstantExpression(OperatorType operatorType, PropertyExpression left,
        ConstantExpression right)
    {
        var property = GetProperty(left.PropertyName);

        if (right.Value is ConstantNullValue)
        {
            return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                Linq.Expression.Constant(null, property.GetType()));
        }

        if (right.Value is not ConstantValue constValue)
        {
            throw new ArgumentException("Unexpected type for right Operand.  Expected ConstantValue");
        }

        switch (constValue.ValueType)
        {
            case ConstantValueType.Boolean:
                if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(constValue.Value, property.PropertyType));
                }

                throw new ArgumentException($"Unable to convert {constValue.Value} to a boolean");
            case ConstantValueType.Number:
                if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    var val = Convert.ToInt32(constValue.Value);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(int?))
                {
                    var val = Convert.ToDecimal(constValue.Value);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                if (property.PropertyType == typeof(double))
                {
                    var val = Convert.ToDouble(constValue.Value);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                throw new ArgumentException($"Unable to convert {constValue.Value} to either int, decimal or double");
            case ConstantValueType.String:
                if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly?))
                {
                    var val = DateOnly.Parse(constValue.Value.ToString()!);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    var val = DateTime.Parse(constValue.Value.ToString()!);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                if (property.PropertyType == typeof(TimeOnly) || property.PropertyType == typeof(TimeOnly?))
                {
                    var val = TimeOnly.Parse(constValue.Value.ToString()!);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                if (property.PropertyType.IsEnum)
                {
                    var val = Enum.Parse(property.PropertyType, constValue.Value.ToString()!, true);
                    return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(val, property.PropertyType));
                }

                return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
                    Linq.Expression.Constant(constValue.Value.ToString()));
            default:
                throw new ArgumentException($"Unhandled ConstantValueType of {constValue.ValueType}");
        }
    }

    private static Linq.Expression GenerateBinaryLinqExpression(OperatorType operatorType, Linq.Expression left,
        Linq.Expression right)
    {
        return operatorType switch
        {
            OperatorType.Equal => Linq.Expression.Equal(left, right),
            OperatorType.NotEqual => Linq.Expression.NotEqual(left, right),
            OperatorType.GreaterThan => Linq.Expression.GreaterThan(left, right),
            OperatorType.GreaterThanOrEqual => Linq.Expression.GreaterThanOrEqual(left, right),
            OperatorType.LessThan => Linq.Expression.LessThan(left, right),
            OperatorType.LessThanOrEqual => Linq.Expression.LessThanOrEqual(left, right),
            _ => throw new ArgumentException($"Unhandled Call Operator {operatorType}")
        };
    }

    private Linq.MemberExpression CompilePropertyExpression(PropertyExpression propExp, PropertyInfo? property = null)
    {
        if (property == null)
        {
            GetProperty(propExp.PropertyName);
        }

        return Linq.Expression.Property(typeParameter, propExp.PropertyName);
    }

    private PropertyInfo GetProperty(string name)
    {
        var prop = properties.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return prop ?? throw new ArgumentException($"No property found with name of {name}");
    }

    private static Linq.Expression CompileConstantExpression(ConstantExpression constExp)
    {
        return constExp.Value switch
        {
            ConstantNullValue => Linq.Expression.Constant(null),
            ConstantValue val => GetConstantValue(val),
            _ => throw new ArgumentException(
                $"Unhandled ConstantExpression of type {constExp.Value.GetType().FullName}")
        };
    }

    private static Linq.Expression GetConstantValue(ConstantValue value)
    {
        return value.ValueType switch
        {
            ConstantValueType.Boolean => Linq.Expression.Constant(value.Value, typeof(bool)),
            ConstantValueType.Number => Linq.Expression.Constant(value.Value, typeof(decimal)),
            ConstantValueType.String => Linq.Expression.Constant(value.Value, typeof(string)),
            _ => throw new ArgumentException($"Unhandled ConstantValueType of {value.ValueType}")
        };
    }
}