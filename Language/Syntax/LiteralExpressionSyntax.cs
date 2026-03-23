namespace Pop.Language;

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken LiteralToken { get; }
    public object? Value => LiteralToken.Value;

    public LiteralExpressionSyntax(SyntaxToken literalToken)
    {
        LiteralToken = literalToken;
    }

    public override SyntaxKind Kind => SyntaxKind.LiteralExpression;
    public override TextSpan Span => LiteralToken.Span;
}
