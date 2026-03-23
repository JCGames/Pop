using System.Collections.Generic;

namespace Pop.Language;

public sealed class LambdaExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken AtToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }
    public BlockStatementSyntax Body { get; }

    public LambdaExpressionSyntax(
        SyntaxToken atToken,
        SyntaxToken openParenToken,
        IReadOnlyList<ParameterSyntax> parameters,
        SyntaxToken closeParenToken,
        BlockStatementSyntax body)
    {
        AtToken = atToken;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CloseParenToken = closeParenToken;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.LambdaExpression;
    public override TextSpan Span => TextSpan.FromBounds(AtToken.Span.Start, Body.Span.End);
}
