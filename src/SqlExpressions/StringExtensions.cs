namespace SqlExpressions;

public static class StringExtensions
{
    public static Where.Ast.Expression ParseWhere(this string value)
    {
        var parser = new Where.Parsing.ExpressionParser();
        return parser.Parse(value);
    }
    
    public static OrderBy.Ast.Expression ParseOrderBy(this string value)
    {
        var parser = new OrderBy.Parsing.OrderByParser();
        return parser.Parse(value);
    }

}