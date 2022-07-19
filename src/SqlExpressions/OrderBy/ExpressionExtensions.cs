using SqlExpressions.OrderBy.Compiling;
using SqlExpressions.OrderBy.Ast;

namespace SqlExpressions.OrderBy;

public static class ExpressionExtensions
{
    public static string Compile(this Expression expression, Func<string, string> propertyMapper)
    {
        var compiler = new OrderByExpressionCompiler();
        return compiler.Compile(expression, propertyMapper);
    }
}