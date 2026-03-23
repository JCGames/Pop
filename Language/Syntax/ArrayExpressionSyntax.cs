using System.Collections.Generic;

namespace Pop.Language;

public sealed class ArrayExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenBracketToken { get; }
    public IReadOnlyList<ExpressionSyntax> Elements { get; }
    public SyntaxToken CloseBracketToken { get; }

    public ArrayExpressionSyntax(
        SyntaxToken openBracketToken,
        IReadOnlyList<ExpressionSyntax> elements,
        SyntaxToken closeBracketToken)
    {
        OpenBracketToken = openBracketToken;
        Elements = elements;
        CloseBracketToken = closeBracketToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayExpression;
    public override TextSpan Span => TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}
