namespace Pop.Language;

public sealed class ElseClauseSyntax : SyntaxNode
{
    public SyntaxToken ElseKeyword { get; }
    public StatementSyntax Statement { get; }

    public ElseClauseSyntax(
        SyntaxToken elseKeyword,
        StatementSyntax statement)
    {
        ElseKeyword = elseKeyword;
        Statement = statement;
    }

    public override SyntaxKind Kind => SyntaxKind.ElseClause;
    public override TextSpan Span => TextSpan.FromBounds(ElseKeyword.Span.Start, Statement.Span.End);
}
