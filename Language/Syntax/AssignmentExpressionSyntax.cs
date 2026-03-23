namespace Pop.Language;

public sealed class AssignmentExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Target { get; }
    public SyntaxToken ArrowToken { get; }
    public ExpressionSyntax Expression { get; }

    public AssignmentExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken arrowToken,
        ExpressionSyntax expression)
    {
        Target = target;
        ArrowToken = arrowToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, Expression.Span.End);
}
