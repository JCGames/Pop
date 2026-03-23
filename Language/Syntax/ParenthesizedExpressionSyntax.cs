namespace Pop.Language;

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenParenToken { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken CloseParenToken { get; }

    public ParenthesizedExpressionSyntax(
        SyntaxToken openParenToken,
        ExpressionSyntax expression,
        SyntaxToken closeParenToken)
    {
        OpenParenToken = openParenToken;
        Expression = expression;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;
    public override TextSpan Span => TextSpan.FromBounds(OpenParenToken.Span.Start, CloseParenToken.Span.End);
}
