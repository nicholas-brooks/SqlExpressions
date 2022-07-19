using SqlExpressions.Where.Ast;
using SqlExpressions.Where.Compiling;

namespace SqlExpressions.Where;

public static class ExpressionExtensions
{
    public static string Compile(this Expression expression, Func<string, string> propertyMapper)
    {
        var compiler = new WhereExpressionCompiler();
        return compiler.Compile(expression, propertyMapper);
    }
}