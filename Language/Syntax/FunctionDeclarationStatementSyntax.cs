using System.Collections.Generic;

namespace Pop.Language;

public sealed class FunctionDeclarationStatementSyntax : StatementSyntax
{
    public SyntaxToken? PublicKeyword { get; }
    public SyntaxToken FunKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }
    public BlockStatementSyntax Body { get; }

    public FunctionDeclarationStatementSyntax(
        SyntaxToken? publicKeyword,
        SyntaxToken funKeyword,
        SyntaxToken identifierToken,
        SyntaxToken openParenToken,
        IReadOnlyList<ParameterSyntax> parameters,
        SyntaxToken closeParenToken,
        BlockStatementSyntax body)
    {
        PublicKeyword = publicKeyword;
        FunKeyword = funKeyword;
        IdentifierToken = identifierToken;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CloseParenToken = closeParenToken;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclarationStatement;
    public override TextSpan Span => TextSpan.FromBounds((PublicKeyword ?? FunKeyword).Span.Start, Body.Span.End);
}
