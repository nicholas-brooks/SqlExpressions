using System.Reflection;
using SqlExpressions.Where.Ast;
using LinqExpression = System.Linq.Expressions.Expression;

namespace SqlExpressions.Where.Compiling;

public class LinqExpressionCompiler
{
    private Type type;
    private PropertyInfo[] properties;

    public LinqExpression Compile<T>(Expression expression)
    {
        type = typeof(T);
        properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.GetProperty);
        return Compile(expression);
    }

    private LinqExpression Compile(Expression expression)
    {
        return expression switch
        {
            CallExpression call => CompileCallExpression(call),
            PropertyExpression propExp => CompilePropertyExpression(propExp),
            ConstantExpression constExp => CompileConstantExpression(constExp),
            //InArrayExpression inArray => CompileInArrayExpression(inArray),
            _ => throw new ArgumentException($"Unhandled Expression of type {expression.GetType().FullName}")
        };
    }

    private LinqExpression CompilePropertyExpression(PropertyExpression propExp)
    {
        return LinqExpression.Property(LinqExpression.Parameter(type, propExp.PropertyName), propExp.PropertyName);
    }

    private LinqExpression CompileCallExpression(CallExpression call)
    {
        return call.OperatorType switch
        {
            OperatorType.Equal => LinqExpression.Equal(Compile(call.Operands[0]), Compile(call.Operands[1])),
            OperatorType.NotEqual => LinqExpression.NotEqual(Compile(call.Operands[0]), Compile(call.Operands[1])),
            OperatorType.GreaterThan => LinqExpression.GreaterThan(Compile(call.Operands[0]), Compile(call.Operands[1])),
            OperatorType.GreaterThanOrEqual => LinqExpression.GreaterThanOrEqual(Compile(call.Operands[0]), Compile(call.Operands[1])),
            OperatorType.LessThan => LinqExpression.LessThan(Compile(call.Operands[0]), Compile(call.Operands[1])),
            OperatorType.LessThanOrEqual => LinqExpression.LessThanOrEqual(Compile(call.Operands[0]), Compile(call.Operands[1])),
            // OperatorType.IsNull => $"{Compile(call.Operands[0])} is null",
            // OperatorType.IsNotNull => $"{Compile(call.Operands[0])} is not null",
            // OperatorType.Like => $"{Compile(call.Operands[0])} like {Compile(call.Operands[1])}",
            // OperatorType.NotLike => $"{Compile(call.Operands[0])} not like {Compile(call.Operands[1])}",
            // OperatorType.And => CompileAndExpression(call.Operands),
            // OperatorType.Or => CompileOrExpression(call.Operands),
            // OperatorType.In => CompileInExpression(call.Operands[0], call.Operands[1]),
            // OperatorType.NotIn => CompileNotInExpression(call.Operands[0], call.Operands[1]),
            _ => throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}")
        };
    }

    private LinqExpression CompileConstantExpression(ConstantExpression constExp)
    {
        return constExp.Value switch
        {
            ConstantNullValue => LinqExpression.Constant(null),
            ConstantValue val => GetConstantValue(val),
            _ => throw new ArgumentException(
                $"Unhandled ConstantExpression of type {constExp.Value.GetType().FullName}")
        };
    }

    private LinqExpression GetConstantValue(ConstantValue value)
    {
        return value.ValueType switch
        {
            ConstantValueType.Boolean => LinqExpression.Constant((bool)value.Value),
            ConstantValueType.Number => LinqExpression.Constant(value.Value),
            ConstantValueType.String => LinqExpression.Constant(value.Value),
            _ => throw new ArgumentException($"Unhandled ConstantValueType of {value.ValueType}")
        };
    }
    
}