namespace SqlExpressions.OrderBy.Parsing;

readonly struct OrderByKeyword
{
    public string Text { get; }
    public OrderByToken Token { get; }

    public OrderByKeyword(string text, OrderByToken token)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Token = token;
    }
}