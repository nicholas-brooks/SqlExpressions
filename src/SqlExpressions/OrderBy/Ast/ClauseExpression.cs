namespace SqlExpressions.OrderBy.Ast;

public class ClauseExpression : Expression
{
    public ClauseExpression(OrderByType type, Expression property)
    {
        OrderByType = type;
        Property = property;
    }

    public OrderByType OrderByType { get; }
    
    public Expression Property { get; }

    public override string ToString()
    {
        return $"{Property} {OrderByType}";
    }
}