using System.Collections.Generic;

namespace Pop.Language;

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndOfFileToken { get; }

    public CompilationUnitSyntax(IReadOnlyList<StatementSyntax> statements, SyntaxToken endOfFileToken)
    {
        Statements = statements;
        EndOfFileToken = endOfFileToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    public override TextSpan Span => Statements.Count == 0
        ? EndOfFileToken.Span
        : TextSpan.FromBounds(Statements[0].Span.Start, EndOfFileToken.Span.End);
}
