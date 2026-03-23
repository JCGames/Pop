namespace Pop.Language;

public sealed class IfStatementSyntax : StatementSyntax
{
    public SyntaxToken IfKeyword { get; }
    public ExpressionSyntax Condition { get; }
    public BlockStatementSyntax ThenStatement { get; }
    public ElseClauseSyntax? ElseClause { get; }

    public IfStatementSyntax(
        SyntaxToken ifKeyword,
        ExpressionSyntax condition,
        BlockStatementSyntax thenStatement,
        ElseClauseSyntax? elseClause)
    {
        IfKeyword = ifKeyword;
        Condition = condition;
        ThenStatement = thenStatement;
        ElseClause = elseClause;
    }

    public override SyntaxKind Kind => SyntaxKind.IfStatement;
    public override TextSpan Span => TextSpan.FromBounds(
        IfKeyword.Span.Start,
        ElseClause?.Span.End ?? ThenStatement.Span.End);
}
