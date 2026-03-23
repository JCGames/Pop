namespace Pop.Language;

public sealed class WhileStatementSyntax : StatementSyntax
{
    public SyntaxToken WhileKeyword { get; }
    public ExpressionSyntax Condition { get; }
    public BlockStatementSyntax Body { get; }

    public WhileStatementSyntax(
        SyntaxToken whileKeyword,
        ExpressionSyntax condition,
        BlockStatementSyntax body)
    {
        WhileKeyword = whileKeyword;
        Condition = condition;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.WhileStatement;
    public override TextSpan Span => TextSpan.FromBounds(WhileKeyword.Span.Start, Body.Span.End);
}
