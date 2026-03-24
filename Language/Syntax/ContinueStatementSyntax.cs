namespace Pop.Language;

public sealed class ContinueStatementSyntax : StatementSyntax
{
    public SyntaxToken SkipKeyword { get; }

    public ContinueStatementSyntax(SyntaxToken skipKeyword)
    {
        SkipKeyword = skipKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ContinueStatement;
    public override TextSpan Span => SkipKeyword.Span;
}
