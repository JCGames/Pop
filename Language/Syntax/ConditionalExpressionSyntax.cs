namespace Pop.Language;

public sealed class ConditionalExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Condition { get; }
    public SyntaxToken QuestionToken { get; }
    public ExpressionSyntax WhenTrue { get; }
    public SyntaxToken ColonToken { get; }
    public ExpressionSyntax WhenFalse { get; }

    public ConditionalExpressionSyntax(
        ExpressionSyntax condition,
        SyntaxToken questionToken,
        ExpressionSyntax whenTrue,
        SyntaxToken colonToken,
        ExpressionSyntax whenFalse)
    {
        Condition = condition;
        QuestionToken = questionToken;
        WhenTrue = whenTrue;
        ColonToken = colonToken;
        WhenFalse = whenFalse;
    }

    public override SyntaxKind Kind => SyntaxKind.ConditionalExpression;
    public override TextSpan Span => TextSpan.FromBounds(Condition.Span.Start, WhenFalse.Span.End);
}
