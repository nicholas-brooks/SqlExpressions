namespace SqlExpressions.Where.Ast;

class PropertyExpression : Expression
{
    readonly bool _requiresEscape;

    public PropertyExpression(string name)
    {
        PropertyName = name ?? throw new ArgumentNullException(nameof(name));
        _requiresEscape = !FilterExpression.IsValidIdentifier(name);
    }

    public string PropertyName { get; }

    public override string ToString()
    {
        if (_requiresEscape)
        {
            return $"@Properties['{FilterExpression.EscapeStringContent(PropertyName)}']";
        }

        return PropertyName;
    }
}