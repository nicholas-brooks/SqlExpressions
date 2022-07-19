using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SqlExpressions.Where.Parsing;

static class ExpressionTextParsers
{
    static readonly TextParser<ExpressionToken> LessOrEqual = Span.EqualTo("<=").Value(ExpressionToken.LessThanOrEqual);
    static readonly TextParser<ExpressionToken> GreaterOrEqual = Span.EqualTo(">=").Value(ExpressionToken.GreaterThanOrEqual);
    static readonly TextParser<ExpressionToken> NotEqual = Span.EqualTo("<>").Value(ExpressionToken.NotEqual);

    public static readonly TextParser<ExpressionToken> CompoundOperator = GreaterOrEqual.Or(LessOrEqual.Try().Or(NotEqual));

    static readonly TextParser<char> StringContentChar =
        Span.EqualTo("''").Value('\'').Try().Or(Character.Except('\''));

    public static readonly TextParser<string> String =
        Character.EqualTo('\'')
            .IgnoreThen(StringContentChar.Many())
            .Then(s => Character.EqualTo('\'').Value(new string(s)));

    public static readonly TextParser<TextSpan> Real =
        Numerics.Integer
            .Then(n => Character.EqualTo('.').IgnoreThen(Numerics.Integer).OptionalOrDefault()
                .Select(f => f == TextSpan.None ? n : new TextSpan(n.Source!, n.Position, n.Length + f.Length + 1)));
}