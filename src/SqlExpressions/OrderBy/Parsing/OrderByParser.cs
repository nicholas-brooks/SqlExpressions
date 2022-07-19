using System.Diagnostics.CodeAnalysis;
using SqlExpressions.OrderBy.Ast;

namespace SqlExpressions.OrderBy.Parsing;

class OrderByParser
{
    readonly OrderByTokenizer _tokenizer = new();

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

        var result = OrderByTokenParser.TryParse(tokenList.Value);
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