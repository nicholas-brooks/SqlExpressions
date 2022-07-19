using SqlExpressions.OrderBy.Ast;

namespace SqlExpressions.OrderBy.Compiling;

public class OrderByExpressionCompiler
{
    private Func<string, string> propertyMapper;

    public OrderByExpressionCompiler()
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
            OrderByClause orderBy => CompileOrderByClause(orderBy),
            PropertyExpression prop => CompilePropertyExpression(prop),
            ClauseExpression clause => CompileClauseExpression(clause),
            _ => throw new ArgumentException($"Unhandled Expression of type {expression.GetType().FullName}")
        };
    }

    private string CompileOrderByClause(OrderByClause orderBy)
    {
        return string.Join(", ", orderBy.Expressions.Select(Compile));
    }

    private string CompilePropertyExpression(PropertyExpression prop)
    {
        return propertyMapper(prop.PropertyName);
    }

    private string CompileClauseExpression(ClauseExpression clause)
    {
        return clause.OrderByType == OrderByType.Descending ?$"{Compile(clause.Property)} desc" : Compile(clause.Property);
    }
}