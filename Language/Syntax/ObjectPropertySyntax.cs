namespace Pop.Language;

public sealed class ObjectPropertySyntax : SyntaxNode
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken ColonToken { get; }
    public ExpressionSyntax Value { get; }

    public ObjectPropertySyntax(SyntaxToken identifierToken, SyntaxToken colonToken, ExpressionSyntax value)
    {
        IdentifierToken = identifierToken;
        ColonToken = colonToken;
        Value = value;
    }

    public override SyntaxKind Kind => SyntaxKind.ObjectProperty;
    public override TextSpan Span => TextSpan.FromBounds(IdentifierToken.Span.Start, Value.Span.End);
}
