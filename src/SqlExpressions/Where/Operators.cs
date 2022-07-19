namespace SqlExpressions.Where;

public enum OperatorType
{
    Like,
    NotLike,
    And,
    Or,
    LessThanOrEqual,
    LessThan,
    GreaterThan,
    GreaterThanOrEqual,
    Equal,
    NotEqual,
    Negate, // not used atm
    Not,
    IsNull,
    IsNotNull,
    In,
    NotIn
}