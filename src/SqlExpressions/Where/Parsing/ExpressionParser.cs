using System.Diagnostics.CodeAnalysis;
using SqlExpressions.Where.Ast;

namespace SqlExpressions.Where.Parsing;

class ExpressionParser
{
    readonly ExpressionTokenizer _tokenizer = new();

    public Expression Parse(string expression)
    {
        if (!TryParse(expression, out var root, out var error))
            throw new ArgumentException(error);

        return root;
    }

    public bool TryParse(string filterExpression,
        [MaybeNullWhen(false)] out Expression root, [MaybeNullWhen(true)] out string error)
    {
        if (filterExpression == null)
        {
            throw new ArgumentNullException(nameof(filterExpression));
        }

        var tokenList = _tokenizer.TryTokenize(filterExpression);
        if (!tokenList.HasValue)
        {
            error = tokenList.ToString();
            root = null;
            return false;
        }

        var result = ExpressionTokenParsers.TryParse(tokenList.Value);
        if (!result.HasValue)
        {
            error = result.ToString();
            root = null;
            return false;
        }

        root = result.Value;
        error = null;
        return true;
    }
}