namespace Pop.Language;

public sealed class BreakStatementSyntax : StatementSyntax
{
    public SyntaxToken AbortKeyword { get; }

    public BreakStatementSyntax(SyntaxToken abortKeyword)
    {
        AbortKeyword = abortKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.BreakStatement;
    public override TextSpan Span => AbortKeyword.Span;
}
