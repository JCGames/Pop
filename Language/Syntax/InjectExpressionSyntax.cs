namespace Pop.Language;

public sealed class InjectExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken InjectKeyword { get; }
    public SyntaxToken PathToken { get; }

    public InjectExpressionSyntax(SyntaxToken injectKeyword, SyntaxToken pathToken)
    {
        InjectKeyword = injectKeyword;
        PathToken = pathToken;
    }

    public override SyntaxKind Kind => SyntaxKind.InjectExpression;
    public override TextSpan Span => TextSpan.FromBounds(InjectKeyword.Span.Start, PathToken.Span.End);
}
