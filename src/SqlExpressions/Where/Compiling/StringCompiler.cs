using SqlExpressions.Where.Ast;

namespace SqlExpressions.Where.Compiling;

/// <summary>
/// Compiles the Expressions AST into a Sql where expression that Dommel will use.
/// e.g. 'OrderNo = '342312' and OrderDate > '2022-01-01' -> "SOOrders"."OrderNo" = '342312' and "SOOrders"."OrderDate" > '2022-01-01'
/// </summary>
internal class StringCompiler
{
    private Func<string, string> propertyMapper;

    public StringCompiler()
    {
        propertyMapper = property => property;
    }
        

    public string Compile(Expression expression, Func<string, string> propertyMapper)
    {
        this.propertyMapper = propertyMapper;
        return Compile(expression);
    }

    private string Compile(Expression expression)
    {
        return expression switch
        {
            CallExpression call => CompileCallExpression(call),
            PropertyExpression propExp => CompilePropertyExpression(propExp),
            ConstantExpression constExp => CompileConstantExpression(constExp),
            InArrayExpression inArray => CompileInArrayExpression(inArray),
            _ => throw new ArgumentException($"Unhandled Expression of type {expression.GetType().FullName}")
        };
    }

    private string CompileCallExpression(CallExpression call)
    {
        return call.OperatorType switch
        {
            OperatorType.Equal => $"{Compile(call.Operands[0])} = {Compile(call.Operands[1])}",
            OperatorType.NotEqual => $"{Compile(call.Operands[0])} <> {Compile(call.Operands[1])}",
            OperatorType.GreaterThan => $"{Compile(call.Operands[0])} > {Compile(call.Operands[1])}",
            OperatorType.GreaterThanOrEqual => $"{Compile(call.Operands[0])} >= {Compile(call.Operands[1])}",
            OperatorType.LessThan => $"{Compile(call.Operands[0])} < {Compile(call.Operands[1])}",
            OperatorType.LessThanOrEqual => $"{Compile(call.Operands[0])} <= {Compile(call.Operands[1])}",
            OperatorType.IsNull => $"{Compile(call.Operands[0])} is null",
            OperatorType.IsNotNull => $"{Compile(call.Operands[0])} is not null",
            OperatorType.Like => $"{Compile(call.Operands[0])} like {Compile(call.Operands[1])}",
            OperatorType.NotLike => $"{Compile(call.Operands[0])} not like {Compile(call.Operands[1])}",
            OperatorType.And => CompileAndExpression(call.Operands),
            OperatorType.Or => CompileOrExpression(call.Operands),
            OperatorType.In => CompileInExpression(call.Operands[0], call.Operands[1]),
            OperatorType.NotIn => CompileNotInExpression(call.Operands[0], call.Operands[1]),
            _ => throw new ArgumentException($"Unhandled Call Operator {call.OperatorType}")
        };
    }
    
    private string CompileInArrayExpression(InArrayExpression inArray)
    {
        return $"({string.Join(",", inArray.Elements.Select(Compile))})";
    }

    private string CompileInExpression(Expression identifier, Expression inArray)
    {
        if (inArray is not InArrayExpression)
        {
            throw new ArgumentException($"Unable to compile In Expression. Expected second operand to be ArrayExpression but is {inArray.GetType().Name}");
        }

        return $"{Compile(identifier)} in {Compile(inArray)}";

    }

    private string CompileNotInExpression(Expression identifier, Expression inArray)
    {
        if (inArray is not InArrayExpression)
        {
            throw new ArgumentException($"Unable to compile Not In Expression. Expected second operand to be ArrayExpression but is {inArray.GetType().Name}");
        }

        return $"{Compile(identifier)} not in {Compile(inArray)}";

    }
        
    private string CompileAndExpression(IEnumerable<Expression> callOperands)
    {
        return string.Join(" and ", callOperands.Select(e => e is CallExpression {IsCompoundOperator: true} ? $"({Compile(e)})" : Compile(e))); 
    }

    private string CompileOrExpression(IEnumerable<Expression> callOperands)
    {
        return string.Join(" or ", callOperands.Select(e => e is CallExpression {IsCompoundOperator: true} ? $"({Compile(e)})" : Compile(e))); 
    }

    private string CompilePropertyExpression(PropertyExpression propExp)
    {
        return propertyMapper(propExp.PropertyName);
    }

    private string CompileConstantExpression(ConstantExpression constExp)
    {
        return constExp.Value switch
        {
            ConstantNullValue => "null",
            ConstantValue val => GetConstantValue(val),
            _ => throw new ArgumentException(
                $"Unhandled ConstantExpression of type {constExp.Value.GetType().FullName}")
        };
    }

    private static string GetConstantValue(ConstantValue value)
    {
        return value.ValueType switch
        {
            ConstantValueType.Boolean => (bool) value.Value ? "1" : "0",
            ConstantValueType.Number => value.Value.ToString() ?? "null",
            ConstantValueType.String => $"'{value.Value}'",
            _ => throw new ArgumentException($"Unhandled ConstantValueType of {value.ValueType}")
        };
    }
        
}