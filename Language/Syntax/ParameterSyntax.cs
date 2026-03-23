namespace Pop.Language;

public sealed class ParameterSyntax : SyntaxNode
{
    public SyntaxToken IdentifierToken { get; }

    public ParameterSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public override SyntaxKind Kind => SyntaxKind.Parameter;
    public override TextSpan Span => IdentifierToken.Span;
}
