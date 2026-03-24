namespace Pop.Language;

public sealed class PostfixUnaryExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Operand { get; }
    public SyntaxToken OperatorToken { get; }

    public PostfixUnaryExpressionSyntax(ExpressionSyntax operand, SyntaxToken operatorToken)
    {
        Operand = operand;
        OperatorToken = operatorToken;
    }

    public override SyntaxKind Kind => SyntaxKind.PostfixUnaryExpression;
    public override TextSpan Span => TextSpan.FromBounds(Operand.Span.Start, OperatorToken.Span.End);
}
