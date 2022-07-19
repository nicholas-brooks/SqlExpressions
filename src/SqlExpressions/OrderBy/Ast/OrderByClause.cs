namespace SqlExpressions.OrderBy.Ast;

public class OrderByClause : Expression
{
    public Expression[] Expressions { get; }

    public OrderByClause(Expression[] expressions)
    {
        Expressions = expressions;
    }

    public override string ToString()
    {
        return $"OrderByClause [{string.Join(", ", Expressions.Select(exp => exp.ToString()))}]";
    }
}