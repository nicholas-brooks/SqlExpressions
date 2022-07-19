namespace SqlExpressions.Where;

/// <summary>
/// Helper methods to assist with construction of well-formed expressions.
/// </summary>
public static class FilterExpression
{
    /// <summary>
    /// Escape a value that is to appear in a `like` expression.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>The text with any special values escaped. Will need to be passed through
    /// <see cref="EscapeStringContent(string)"/> if it is being embedded directly into a filter expression.</returns>
    // ReSharper disable once UnusedMember.Global
    public static string EscapeLikeExpressionContent(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return EscapeStringContent(text)
            .Replace("%", "%%")
            .Replace("_", "__");
    }

    /// <summary>
    /// Escape a fragment of text that will appear within a string.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>The text with any special values escaped.</returns>
    public static string EscapeStringContent(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return text.Replace("'", "''");
    }

    /// <summary>
    /// Determine if the specified text is a valid identifier.
    /// </summary>
    /// <param name="identifier">The text to check.</param>
    /// <returns>True if the text can be used verbatim as a property name.</returns>
    public static bool IsValidIdentifier(string identifier)
    {
        return identifier.Length != 0 &&
               !char.IsDigit(identifier[0]) &&
               identifier.All(ch => char.IsLetter(ch) || char.IsDigit(ch) || ch == '_');
    }
}