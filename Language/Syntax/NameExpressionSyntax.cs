namespace Pop.Language;

public sealed class NameExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken IdentifierToken { get; }

    public NameExpressionSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public override SyntaxKind Kind => SyntaxKind.NameExpression;
    public override TextSpan Span => IdentifierToken.Span;
}
