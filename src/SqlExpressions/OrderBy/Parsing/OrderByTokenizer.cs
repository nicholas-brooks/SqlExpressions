using Superpower;
using Superpower.Model;

namespace SqlExpressions.OrderBy.Parsing;

public class OrderByTokenizer : Tokenizer<OrderByToken>
{
    readonly OrderByKeyword[] keywords =
    {
        new("asc", OrderByToken.Ascending),
        new("desc", OrderByToken.Descending)
    };
    
    protected override IEnumerable<Result<OrderByToken>> Tokenize(TextSpan stringSpan)
    {
            var next = SkipWhiteSpace(stringSpan);
            if (!next.HasValue)
            {
                yield break;
            }

            do
            {
                if (next.Value == '"')
                {
                    var str = OrderByTextParsers.QuotedString(next.Location);
                    if (!str.HasValue)
                    {
                        yield return Result.CastEmpty<string, OrderByToken>(str);
                    }

                    next = str.Remainder.ConsumeChar();

                    yield return Result.Value(OrderByToken.Identifier, str.Location, str.Remainder);
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
                        yield return Result.Value(OrderByToken.Identifier, beginIdentifier, next.Location);
                    }
                }
                else if (next.Value == ',')
                {
                    yield return Result.Value(OrderByToken.Comma, next.Location, next.Remainder);
                    next = next.Remainder.ConsumeChar();
                }
                else
                {
                    yield return Result.Empty<OrderByToken>(next.Location);
                    next = next.Remainder.ConsumeChar();
                }

                next = SkipWhiteSpace(next.Location);
            } while (next.HasValue);        
    }
    
    bool TryGetKeyword(TextSpan span, out OrderByToken keyword)
    {
        foreach (var kw in keywords)
        {
            if (span.EqualsValueIgnoreCase(kw.Text))
            {
                keyword = kw.Token;
                return true;
            }
        }

        keyword = OrderByToken.None;
        return false;
    }    
}