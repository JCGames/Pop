namespace Pop.Language;

public sealed class ReturnStatementSyntax : StatementSyntax
{
    public SyntaxToken RetKeyword { get; }
    public ExpressionSyntax? Expression { get; }

    public ReturnStatementSyntax(SyntaxToken retKeyword, ExpressionSyntax? expression)
    {
        RetKeyword = retKeyword;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.ReturnStatement;
    public override TextSpan Span => TextSpan.FromBounds(
        RetKeyword.Span.Start,
        Expression?.Span.End ?? RetKeyword.Span.End);
}
