using Superpower.Display;

namespace SqlExpressions.OrderBy.Parsing;

public enum OrderByToken
{
    None,
    Identifier,
    [Token(Example = ",")]
    Comma,
    [Token(Category = "modifier", Example = "asc")]
    Ascending,
    [Token(Category = "modifier", Example = "desc")]
    Descending
}