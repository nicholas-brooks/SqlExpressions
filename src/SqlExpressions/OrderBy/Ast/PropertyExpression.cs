namespace SqlExpressions.OrderBy.Ast;

public class PropertyExpression : Expression
    {
        public PropertyExpression(string name)
        {
            PropertyName = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string PropertyName { get; }

        public override string ToString()
        {
            return PropertyName;
        }
    }