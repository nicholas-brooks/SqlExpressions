using SqlExpressions.Where.Ast;
using SqlExpressions.Where.Compiling;
using Linq = System.Linq.Expressions;

namespace SqlExpressions.Where;

public static class ExpressionExtensions
{
    public static string CompileToString(this Expression expression, Func<string, string> propertyMapper)
    {
        var compiler = new StringCompiler();
        return compiler.Compile(expression, propertyMapper);
    }

    public static Linq.Expression<Func<T, bool>> CompileToLinq<T>(this Expression expression)
    {
        var compiler = new LinqExpressionCompiler();
        return compiler.Compile<T>(expression);
    }
}