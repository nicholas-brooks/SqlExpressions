using System.Reflection;
using SqlExpressions.Where.Ast;
using Linq = System.Linq.Expressions;

namespace SqlExpressions.Where.Compiling;

public class LinqExpressionCompiler
{
    private Type type;
    private PropertyInfo[] properties;
    private Linq.ParameterExpression typeParameter;

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
            default:
                throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}");
        }
        return call.OperatorType switch
        {
            // OperatorType.Like => $"{Compile(call.Operands[0])} like {Compile(call.Operands[1])}",
            // OperatorType.NotLike => $"{Compile(call.Operands[0])} not like {Compile(call.Operands[1])}",
            // OperatorType.And => CompileAndExpression(call.Operands),
            // OperatorType.Or => CompileOrExpression(call.Operands),
            // OperatorType.In => CompileInExpression(call.Operands[0], call.Operands[1]),
            // OperatorType.NotIn => CompileNotInExpression(call.Operands[0], call.Operands[1]),
            _ => throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}")
        };
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

             return Linq.Expression.IsTrue(Linq.Expression.Constant(false)); // expression is false as Property Type is not null.
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

             return Linq.Expression.IsTrue(Linq.Expression.Constant(true)); // expression will always be true as Property Type is not nullable.
         }

         throw new ArgumentException(
             $"Unable to compile Is Null Expression.  Expected operand to be PropertyExpression but is {identifier.GetType().Name}");

     }
     
    

    private Linq.Expression CompileBinaryExpression(CallExpression call, Expression left, Expression right)
    {
        // we have to figure out what type the property operand is for and attempt to convert the other side to that type. 
        if (right is PropertyExpression && left is ConstantExpression)
        {
            return CompileBinaryConstantExpression(call.OperatorType, (PropertyExpression) right, (ConstantExpression) left);
        }
        else if (right is ConstantExpression && left is PropertyExpression)
        {
            return CompileBinaryConstantExpression(call.OperatorType, (PropertyExpression) left, (ConstantExpression) right);
        }
        else if (right is PropertyExpression && left is PropertyExpression)
        {
            return CompileBinaryExpression(call.OperatorType, CompilePropertyExpression((PropertyExpression) left), CompilePropertyExpression((PropertyExpression) right));
        }

        throw new ArgumentException("Expected left and right to be either PropertyExpression or ConstantExpression");
    }

    private Linq.Expression CompileBinaryConstantExpression(OperatorType operatorType, PropertyExpression left, ConstantExpression right)
    {
        var property = GetProperty(left.PropertyName);
        
        if (right.Value is ConstantNullValue)
        {
            return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property), Linq.Expression.Constant(null, property.GetType()));
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
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property),
                        Linq.Expression.Constant(constValue.Value, property.PropertyType));
                }

                throw new ArgumentException($"Unable to convert {constValue.Value} to a boolean");
            case ConstantValueType.Number:
                if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    var val = Convert.ToInt32(constValue.Value);
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property),  Linq.Expression.Constant(val, property.PropertyType));
                }
                if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(int?))
                {
                    var val = Convert.ToDecimal(constValue.Value);
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property),  Linq.Expression.Constant(val, property.PropertyType));
                }
                if (property.PropertyType == typeof(double))
                {
                    var val = Convert.ToDouble(constValue.Value);
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property),  Linq.Expression.Constant(val, property.PropertyType));
                }
                throw new ArgumentException($"Unable to convert {constValue.Value} to either int, deciaml or double");
            case ConstantValueType.String:
                if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly?))
                {
                    var val = DateOnly.Parse(constValue.Value.ToString()!);
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property), Linq.Expression.Constant(val, property.PropertyType));
                }
                if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    var val = DateTime.Parse(constValue.Value.ToString()!);
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property), Linq.Expression.Constant(val, property.PropertyType));
                }
                if (property.PropertyType == typeof(TimeOnly) || property.PropertyType == typeof(TimeOnly?))
                {
                    var val = TimeOnly.Parse(constValue.Value.ToString()!);
                    return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property), Linq.Expression.Constant(val, property.PropertyType));
                }
                return CompileBinaryExpression(operatorType, CompilePropertyExpression(left, property), Linq.Expression.Constant(constValue.Value.ToString()));
            default:
                throw new ArgumentException($"Unhandled ConstantValueType of {constValue.ValueType}");
        }
    }

    private Linq.Expression CompileBinaryExpression(OperatorType operatorType, Linq.Expression left, Linq.Expression right)
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
}