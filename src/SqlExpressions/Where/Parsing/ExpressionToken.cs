using Superpower.Display;

namespace SqlExpressions.Where.Parsing;

public enum ExpressionToken
{
    None,
    Identifier,
    String,
    Number,

    [Token(Example = ",")]
    Comma,

    [Token(Example = "[")]
    LBracket,

    [Token(Example = "]")]
    RBracket,

    [Token(Example = "(")]
    LParen,

    [Token(Example = ")")]
    RParen,
        
    [Token(Category = "operator", Example = "<")]
    LessThan,

    [Token(Category = "operator", Example = "<=")]
    LessThanOrEqual,

    [Token(Category = "operator", Example = ">")]
    GreaterThan,

    [Token(Category = "operator", Example = ">=")]
    GreaterThanOrEqual,

    [Token(Category = "operator", Example = "=")]
    Equal,

    [Token(Category = "operator", Example = "<>")]
    NotEqual,

    [Token(Category = "keyword", Example = "and")]
    And,

    [Token(Category = "keyword", Example = "is")]
    Is,

    [Token(Category = "keyword", Example = "like")]
    Like,

    [Token(Category = "keyword", Example = "not")]
    Not,

    [Token(Category = "keyword", Example = "or")]
    Or,

    [Token(Category = "keyword", Example = "true")]
    True,

    [Token(Category = "keyword", Example = "false")]
    False,

    [Token(Category = "keyword", Example = "null")]
    Null,

    [Token(Category = "keyword", Example = "ci")]
    CI,
        
    [Token(Category = "keyword", Example = "in")]
    In
}