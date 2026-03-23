namespace Pop.Language;

public sealed class MemberAccessExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Target { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken IdentifierToken { get; }

    public MemberAccessExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken dotToken,
        SyntaxToken identifierToken)
    {
        Target = target;
        DotToken = dotToken;
        IdentifierToken = identifierToken;
    }

    public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, IdentifierToken.Span.End);
}
