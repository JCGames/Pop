namespace Pop.Language;

public sealed class BreakStatementSyntax : StatementSyntax
{
    public SyntaxToken BreakKeyword { get; }

    public BreakStatementSyntax(SyntaxToken breakKeyword)
    {
        BreakKeyword = breakKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.BreakStatement;
    public override TextSpan Span => BreakKeyword.Span;
}
