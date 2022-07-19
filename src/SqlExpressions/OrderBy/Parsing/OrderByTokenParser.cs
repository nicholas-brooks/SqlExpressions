using SqlExpressions.OrderBy.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SqlExpressions.OrderBy.Parsing;

public static class OrderByTokenParser
{
    public static TokenListParserResult<OrderByToken, OrderByClause> TryParse(
        TokenList<OrderByToken> input)
    {
        return Expr.AtEnd().TryParse(input);
    }

    private static readonly TokenListParser<OrderByToken, Expression> Identifier =
        Token.EqualTo(OrderByToken.Identifier)
            .Select(t => (Expression)new PropertyExpression(t.ToStringValue()))
            .Named("property");

    private static readonly TokenListParser<OrderByToken, Expression> Call =
        Identifier.Then(modifier =>
            Token.EqualTo(OrderByToken.Ascending).Value(OrderByType.Ascending)
                .Or(Token.EqualTo(OrderByToken.Descending).Value(OrderByType.Descending))
                .Select(op => (Expression)new ClauseExpression(op, modifier))
                .OptionalOrDefault((Expression)new ClauseExpression(OrderByType.Ascending, modifier)));


    private static readonly TokenListParser<OrderByToken, OrderByClause> Expr =
        from clauses in Call.ManyDelimitedBy(Token.EqualTo(OrderByToken.Comma))
        select new OrderByClause(clauses);
}