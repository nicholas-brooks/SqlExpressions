using System.Globalization;
using SqlExpressions.Where.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SqlExpressions.Where.Parsing;

static class ExpressionTokenParsers
{
    public static TokenListParserResult<ExpressionToken, Expression> TryParse(
        TokenList<ExpressionToken> input)
    {
        return Expr.AtEnd().TryParse(input);
    }

    static readonly TokenListParser<ExpressionToken, OperatorType> And = Token.EqualTo(ExpressionToken.And)
        .Value(OperatorType.And);

    static readonly TokenListParser<ExpressionToken, OperatorType> Or = Token.EqualTo(ExpressionToken.Or)
        .Value(OperatorType.Or);

    static readonly TokenListParser<ExpressionToken, OperatorType> Lte =
        Token.EqualTo(ExpressionToken.LessThanOrEqual).Value(OperatorType.LessThanOrEqual);

    static readonly TokenListParser<ExpressionToken, OperatorType> Lt = Token.EqualTo(ExpressionToken.LessThan)
        .Value(OperatorType.LessThan);

    static readonly TokenListParser<ExpressionToken, OperatorType> Gt = Token.EqualTo(ExpressionToken.GreaterThan)
        .Value(OperatorType.GreaterThan);

    static readonly TokenListParser<ExpressionToken, OperatorType> Gte =
        Token.EqualTo(ExpressionToken.GreaterThanOrEqual).Value(OperatorType.GreaterThanOrEqual);

    static readonly TokenListParser<ExpressionToken, OperatorType> Eq = Token.EqualTo(ExpressionToken.Equal)
        .Value(OperatorType.Equal);

    static readonly TokenListParser<ExpressionToken, OperatorType> Neq = Token.EqualTo(ExpressionToken.NotEqual)
        .Value(OperatorType.NotEqual);

    static readonly TokenListParser<ExpressionToken, OperatorType> Not = Token.EqualTo(ExpressionToken.Not)
        .Value(OperatorType.Not);

    static readonly TokenListParser<ExpressionToken, OperatorType> Like = Token.EqualTo(ExpressionToken.Like)
        .Value(OperatorType.Like);

    static readonly TokenListParser<ExpressionToken, OperatorType> NotLike =
        Token.EqualTo(ExpressionToken.Not)
            .IgnoreThen(Token.EqualTo(ExpressionToken.Like))
            .Value(OperatorType.NotLike);

    static readonly TokenListParser<ExpressionToken, OperatorType> In = Token.EqualTo(ExpressionToken.In)
        .Value(OperatorType.In);

    static readonly TokenListParser<ExpressionToken, OperatorType> NotIn =
        Token.EqualTo(ExpressionToken.Not)
            .IgnoreThen(Token.EqualTo(ExpressionToken.In))
            .Value(OperatorType.NotIn);

    static readonly TokenListParser<ExpressionToken, Expression> RootProperty =
        Token.EqualTo(ExpressionToken.Identifier)
            .Select(t => (Expression)new PropertyExpression(t.ToStringValue()))
            .Named("property");

    static readonly TokenListParser<ExpressionToken, Expression> String =
        Token.EqualTo(ExpressionToken.String)
            .Apply(ExpressionTextParsers.String)
            .Select(s => (Expression)new ConstantExpression(new ConstantValue(ConstantValueType.String, s)));

    static readonly TokenListParser<ExpressionToken, Expression> Number =
        Token.EqualTo(ExpressionToken.Number)
            .Apply(ExpressionTextParsers.Real)
            .SelectCatch(n => decimal.Parse(n.ToStringValue(), CultureInfo.InvariantCulture),
                "the numeric literal is too large")
            .Select(d => (Expression)new ConstantExpression(new ConstantValue(ConstantValueType.Number, d)));

    static readonly TokenListParser<ExpressionToken, Expression> Literal =
        String
            .Or(Number)
            .Or(Token.EqualTo(ExpressionToken.True)
                .Value((Expression)new ConstantExpression(new ConstantValue(ConstantValueType.Boolean, true))))
            .Or(Token.EqualTo(ExpressionToken.False)
                .Value((Expression)new ConstantExpression(new ConstantValue(ConstantValueType.Boolean, false))))
            .Or(Token.EqualTo(ExpressionToken.Null)
                .Value((Expression)new ConstantExpression(new ConstantNullValue())))
            .Named("literal");

    static readonly TokenListParser<ExpressionToken, Expression> InLiteral =
        (from lbracket in Token.EqualTo(ExpressionToken.LBracket)
            from elements in Literal.AtLeastOnceDelimitedBy(Token.EqualTo(ExpressionToken.Comma))
            from rbracket in Token.EqualTo(ExpressionToken.RBracket)
            select (Expression)new InArrayExpression(elements)).Named("in-array");

    static readonly TokenListParser<ExpressionToken, Expression> Item =
        Literal
            .Or(RootProperty)
            .Or(InLiteral)
            .Named("Item");

    static readonly TokenListParser<ExpressionToken, Expression> Factor =
        (from lparen in Token.EqualTo(ExpressionToken.LParen)
            from expr in Parse.Ref(() => Expr!)
            from rparen in Token.EqualTo(ExpressionToken.RParen)
            select expr)
        .Or(Item);

    static readonly TokenListParser<ExpressionToken, Expression> Operand =
        (from op in Not
            from factor in Factor
            select MakeUnary(op, factor))
        .Or(Factor)
        .Then(operand => Token.EqualTo(ExpressionToken.Is).Try()
            .IgnoreThen(
                Token.EqualTo(ExpressionToken.Null).Value(OperatorType.IsNull)
                    .Or(Token.EqualTo(ExpressionToken.Not).IgnoreThen(Token.EqualTo(ExpressionToken.Null))
                        .Value(OperatorType.IsNotNull)))
            .Select(op => (Expression)new CallExpression(op, operand))
            .OptionalOrDefault(operand))
        .Named("expression");

    static readonly TokenListParser<ExpressionToken, Expression> Comparison = Parse.Chain(
        NotLike.Try().Or(Like)
            .Or(NotIn.Try().Or(In))
            .Or(Lte.Or(Neq).Or(Lt))
            .Or(Gte.Or(Gt))
            .Or(Eq),
        Operand,
        MakeBinary);

    // we treat AND and OR as equal in priority
    static readonly TokenListParser<ExpressionToken, Expression> Connectives =
        Parse.Chain(And.Or(Or), Comparison, MakeBinary);

    private static readonly TokenListParser<ExpressionToken, Expression> Expr = Connectives;

    private static Expression MakeBinary(OperatorType operatorType, Expression leftOperand, Expression rightOperand)
    {
        return new CallExpression(operatorType, leftOperand, rightOperand);
    }

    private static Expression MakeUnary(OperatorType operatorType, Expression operand)
    {
        return new CallExpression(operatorType, operand);
    }
}