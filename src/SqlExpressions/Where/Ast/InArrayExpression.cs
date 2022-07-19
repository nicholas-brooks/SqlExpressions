namespace SqlExpressions.Where.Ast;

public class InArrayExpression : Expression
{
    public InArrayExpression(Expression[] elements)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }

    public Expression[] Elements { get; }

    public override string ToString()
    {
        return "(" + string.Join(", ", Elements.Select(o => o.ToString())) + ")";
    }
}