using Superpower;
using Superpower.Parsers;

namespace SqlExpressions.OrderBy.Parsing;

public static class OrderByTextParsers
{
    public static readonly TextParser<string> QuotedString =
        Character.EqualTo('"')
            .IgnoreThen(Span.Except("\""))
            .Then(s => Character.EqualTo('"').Value(s.ToStringValue()));
}