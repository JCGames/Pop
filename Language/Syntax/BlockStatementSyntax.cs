using System.Collections.Generic;

namespace Pop.Language;

public sealed class BlockStatementSyntax : StatementSyntax
{
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken CloseBraceToken { get; }

    public BlockStatementSyntax(
        SyntaxToken openBraceToken,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken closeBraceToken)
    {
        OpenBraceToken = openBraceToken;
        Statements = statements;
        CloseBraceToken = closeBraceToken;
    }

    public override SyntaxKind Kind => SyntaxKind.BlockStatement;
    public override TextSpan Span => TextSpan.FromBounds(OpenBraceToken.Span.Start, CloseBraceToken.Span.End);
}
