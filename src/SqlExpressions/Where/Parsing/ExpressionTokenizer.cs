using Superpower;
using Superpower.Model;

namespace SqlExpressions.Where.Parsing;

class ExpressionTokenizer : Tokenizer<ExpressionToken>
{
    readonly ExpressionToken[] _singleCharOps = new ExpressionToken[128];

    readonly ExpressionKeyword[] _keywords =
    {
        new("and", ExpressionToken.And),
        new("is", ExpressionToken.Is),
        new("in", ExpressionToken.In),
        new("like", ExpressionToken.Like),
        new("not", ExpressionToken.Not),
        new("or", ExpressionToken.Or),
        new("true", ExpressionToken.True),
        new("false", ExpressionToken.False),
        new("null", ExpressionToken.Null),
        new("ci", ExpressionToken.CI),
    };

    public ExpressionTokenizer()
    {
        _singleCharOps['<'] = ExpressionToken.LessThan;
        _singleCharOps['>'] = ExpressionToken.GreaterThan;
        _singleCharOps['='] = ExpressionToken.Equal;
        _singleCharOps[','] = ExpressionToken.Comma;
        _singleCharOps['('] = ExpressionToken.LParen;
        _singleCharOps[')'] = ExpressionToken.RParen;
        _singleCharOps['['] = ExpressionToken.LBracket;
        _singleCharOps[']'] = ExpressionToken.RBracket;
    }

    protected override IEnumerable<Result<ExpressionToken>> Tokenize(TextSpan stringSpan)
    {
        var next = SkipWhiteSpace(stringSpan);
        if (!next.HasValue)
        {
            yield break;
        }

        do
        {
            if (char.IsDigit(next.Value))
            {
                var real = ExpressionTextParsers.Real(next.Location);
                if (!real.HasValue)
                {
                    yield return Result.CastEmpty<TextSpan, ExpressionToken>(real);
                }
                else
                {
                    yield return Result.Value(ExpressionToken.Number, real.Location, real.Remainder);
                }

                next = real.Remainder.ConsumeChar();

                if (!IsDelimiter(next))
                {
                    yield return Result.Empty<ExpressionToken>(next.Location, new[] { "digit" });
                }
            }
            else if (next.Value == '\'')
            {
                var str = ExpressionTextParsers.String(next.Location);
                if (!str.HasValue)
                {
                    yield return Result.CastEmpty<string, ExpressionToken>(str);
                }

                next = str.Remainder.ConsumeChar();

                yield return Result.Value(ExpressionToken.String, str.Location, str.Remainder);
            }
            else if (char.IsLetter(next.Value) || next.Value == '_')
            {
                var beginIdentifier = next.Location;
                do
                {
                    next = next.Remainder.ConsumeChar();
                }
                while (next.HasValue && (char.IsLetterOrDigit(next.Value) || next.Value == '_'));

                if (TryGetKeyword(beginIdentifier.Until(next.Location), out var keyword))
                {
                    yield return Result.Value(keyword, beginIdentifier, next.Location);
                }
                else
                {
                    yield return Result.Value(ExpressionToken.Identifier, beginIdentifier, next.Location);
                }
            }
            else
            {
                var compoundOp = ExpressionTextParsers.CompoundOperator(next.Location);
                if (compoundOp.HasValue)
                {
                    yield return Result.Value(compoundOp.Value, compoundOp.Location, compoundOp.Remainder);
                    next = compoundOp.Remainder.ConsumeChar();
                }
                else if (next.Value < _singleCharOps.Length && _singleCharOps[next.Value] != ExpressionToken.None)
                {
                    yield return Result.Value(_singleCharOps[next.Value], next.Location, next.Remainder);
                    next = next.Remainder.ConsumeChar();
                }
                else
                {
                    yield return Result.Empty<ExpressionToken>(next.Location);
                    next = next.Remainder.ConsumeChar();
                }
            }

            next = SkipWhiteSpace(next.Location);
        } while (next.HasValue);
    }

    bool IsDelimiter(Result<char> next)
    {
        return !next.HasValue ||
               char.IsWhiteSpace(next.Value) ||
               next.Value < _singleCharOps.Length && _singleCharOps[next.Value] != ExpressionToken.None;
    }

    bool TryGetKeyword(TextSpan span, out ExpressionToken keyword)
    {
        foreach (var kw in _keywords)
        {
            if (span.EqualsValueIgnoreCase(kw.Text))
            {
                keyword = kw.Token;
                return true;
            }
        }

        keyword = ExpressionToken.None;
        return false;
    }
}