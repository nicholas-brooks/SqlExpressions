namespace SqlExpressions.Where.Ast;

public enum ConstantValueType
{
    Number,
    String,
    Boolean
}

public abstract record ConstantAbstractValue
{
}

public record ConstantValue(ConstantValueType ValueType, object Value) : ConstantAbstractValue;


public record ConstantNullValue : ConstantAbstractValue;


public class ConstantExpression : Expression
{
    public ConstantExpression(ConstantAbstractValue value)
    {
        this.Value = value;
    }

    public ConstantAbstractValue Value { get; }

    public override string ToString()
    {
        return Value.ToString();
    }
}