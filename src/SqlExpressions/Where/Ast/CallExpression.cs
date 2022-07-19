namespace SqlExpressions.Where.Ast;

class CallExpression : Expression
{
    public CallExpression(OperatorType operatorType, params Expression[] operands)
    {
        OperatorType = operatorType;
        Operands = operands ?? throw new ArgumentNullException(nameof(operands));
    }

    public OperatorType OperatorType { get; }

    public bool IsCompoundOperator => OperatorType is OperatorType.And or OperatorType.Or;

    public Expression[] Operands { get; }

    public override string ToString()
    {
        return $"{OperatorType}({string.Join(", ", Operands.Select(o => o.ToString()))})";
    }
}