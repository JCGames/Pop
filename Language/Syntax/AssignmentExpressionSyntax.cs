namespace Pop.Language;

public sealed class AssignmentExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken ArrowToken { get; }
    public ExpressionSyntax Expression { get; }

    public AssignmentExpressionSyntax(
        SyntaxToken identifierToken,
        SyntaxToken arrowToken,
        ExpressionSyntax expression)
    {
        IdentifierToken = identifierToken;
        ArrowToken = arrowToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
    public override TextSpan Span => TextSpan.FromBounds(IdentifierToken.Span.Start, Expression.Span.End);
}
