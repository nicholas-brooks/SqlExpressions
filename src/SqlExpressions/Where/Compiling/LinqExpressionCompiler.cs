using System.Reflection;
using SqlExpressions.Where.Ast;
using Linq = System.Linq.Expressions;

namespace SqlExpressions.Where.Compiling;

public sealed class LinqExpressionCompiler
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
            case OperatorType.And:
                return Linq.Expression.AndAlso(Compile(call.Operands[0]), Compile(call.Operands[1]));
            case OperatorType.Or:
                return Linq.Expression.OrElse(Compile(call.Operands[0]), Compile(call.Operands[1]));
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
            case OperatorType.In:
                return CompileInExpression(call.Operands[0], call.Operands[1]);
            case OperatorType.NotIn:
                return CompileNotInExpression(call.Operands[0], call.Operands[1]);
            default:
                throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}");
        }
    }

    private static readonly MethodInfo ContainsMethodForIn =
        typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => string.Equals("Contains", m.Name) && m.GetParameters().Length == 2);

    private Linq.Expression CompileInExpression(Expression left, Expression right)
    {
        if (left is PropertyExpression propertyExpression && right is InArrayExpression arrayExpression)
        {
            var property = GetProperty(propertyExpression.PropertyName);

            if (arrayExpression.Elements.Length == 0)
            {
                return Linq.Expression.Constant(false);
            }

            var values = arrayExpression.Elements.Select(exp =>
            {
                if (exp is ConstantExpression {Value: ConstantValue constantValue})
                {
                    return Linq.Expression.Constant(PropertyConstantValueToValue(property, constantValue),
                        property.PropertyType);
                }

                throw new ArgumentException(
                    $"Unable to convert {exp} to {property.PropertyType.Name} for {property.Name}");
            }).ToArray();

            return Linq.Expression.Call(
                ContainsMethodForIn.MakeGenericMethod(property.PropertyType),
                Linq.Expression.NewArrayInit(property.PropertyType, values),
                CompilePropertyExpression(propertyExpression, property));
        }

        throw new ArgumentException(
            $"Unable to compile In Expression.  Expected left to be PropertyExpression and right to be InArrayExpression but was {left.GetType().Name} and {right.GetType().Name}");
    }

    private Linq.Expression CompileNotInExpression(Expression left, Expression right)
    {
        return Linq.Expression.Not(CompileInExpression(left, right));
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

            if (constantExpression.Value is ConstantValue {ValueType: ConstantValueType.String} constantValue)
            {
                var value = constantValue.Value.ToString() ?? "";

                switch (value)
                {
                    case "":
                        return Linq.Expression.Equal(CompilePropertyExpression(propertyExpression, prop),
                            Linq.Expression.Constant(""));
                    case "%":
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

        return GenerateBinaryLinqExpression(operatorType, CompilePropertyExpression(left, property),
            CompileConstantExpression(property, right));
    }

    private Linq.Expression CompileConstantExpression(PropertyInfo property, ConstantExpression expression)
    {
        if (expression.Value is ConstantNullValue)
        {
            return Linq.Expression.Constant(null, property.GetType());
        }

        if (expression.Value is not ConstantValue constValue)
        {
            throw new ArgumentException("Unexpected type for right Operand.  Expected ConstantValue");
        }

        return Linq.Expression.Constant(PropertyConstantValueToValue(property, constValue), property.PropertyType);
    }

    private static object PropertyConstantValueToValue(PropertyInfo property, ConstantValue value)
    {
        var propertyType = property.PropertyType;

        if (propertyType == typeof(bool) || propertyType == typeof(bool?))
        {
            if (value.ValueType == ConstantValueType.Boolean)
                return value.Value;
            return Convert.ToBoolean(value.Value);
        }

        if (propertyType == typeof(int) || propertyType == typeof(int?))
        {
            return Convert.ToInt32(value.Value);
        }

        if (propertyType == typeof(decimal) || propertyType == typeof(int?))
        {
            return Convert.ToDecimal(value.Value);
        }

        if (propertyType == typeof(long) || propertyType == typeof(long?))
        {
            return Convert.ToInt64(value.Value);
        }

        if (propertyType == typeof(double))
        {
            return Convert.ToDouble(value.Value);
        }

        if (propertyType == typeof(string))
        {
            return value.Value.ToString()!;
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            return DateTime.Parse(value.Value.ToString()!);
        }

        if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
        {
            return DateTimeOffset.Parse(value.Value.ToString()!);
        }

        if (propertyType == typeof(DateOnly) || propertyType == typeof(DateOnly?))
        {
            return DateOnly.Parse(value.Value.ToString()!);
        }

        if (propertyType == typeof(TimeOnly) || propertyType == typeof(TimeOnly?))
        {
            return TimeOnly.Parse(value.Value.ToString()!);
        }

        if (propertyType == typeof(Guid))
        {
            return Guid.Parse(value.Value.ToString()!);
        }

        if (propertyType.IsEnum)
        {
            return Enum.Parse(propertyType, value.Value.ToString()!, true);
        }

        throw new ArgumentException($"Unable to convert {value.Value} to type {propertyType.Name} for {property.Name}");
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