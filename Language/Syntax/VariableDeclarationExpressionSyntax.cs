namespace Pop.Language;

public sealed class VariableDeclarationExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken? PublicKeyword { get; }
    public SyntaxToken VarKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken ArrowToken { get; }
    public ExpressionSyntax Initializer { get; }

    public VariableDeclarationExpressionSyntax(
        SyntaxToken? publicKeyword,
        SyntaxToken varKeyword,
        SyntaxToken identifierToken,
        SyntaxToken arrowToken,
        ExpressionSyntax initializer)
    {
        PublicKeyword = publicKeyword;
        VarKeyword = varKeyword;
        IdentifierToken = identifierToken;
        ArrowToken = arrowToken;
        Initializer = initializer;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclarationExpression;
    public override TextSpan Span => TextSpan.FromBounds((PublicKeyword ?? VarKeyword).Span.Start, Initializer.Span.End);
}
