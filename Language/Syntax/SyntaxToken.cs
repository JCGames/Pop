namespace Pop.Language;

public sealed class SyntaxToken
{
    public SyntaxKind Kind { get; }
    public TextSpan Span { get; }
    public string Text { get; }
    public object? Value { get; }

    public SyntaxToken(SyntaxKind kind, TextSpan span, string text, object? value = null)
    {
        Kind = kind;
        Span = span;
        Text = text;
        Value = value;
    }

    public override string ToString() => $"{Kind}: {Text}";
}
