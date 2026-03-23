namespace Pop.Language;

public sealed class ContinueStatementSyntax : StatementSyntax
{
    public SyntaxToken ContKeyword { get; }

    public ContinueStatementSyntax(SyntaxToken contKeyword)
    {
        ContKeyword = contKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ContinueStatement;
    public override TextSpan Span => ContKeyword.Span;
}
