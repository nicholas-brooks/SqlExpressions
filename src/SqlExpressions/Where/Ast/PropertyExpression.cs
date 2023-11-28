namespace SqlExpressions.Where.Ast;

class PropertyExpression : Expression
{
    readonly bool requiresEscape;

    public PropertyExpression(string name)
    {
        PropertyName = name ?? throw new ArgumentNullException(nameof(name));
        requiresEscape = !FilterExpression.IsValidIdentifier(name);
    }

    public string PropertyName { get; }

    public override string ToString()
    {
        if (requiresEscape)
        {
            return $"@Properties['{FilterExpression.EscapeStringContent(PropertyName)}']";
        }

        return PropertyName;
    }
}