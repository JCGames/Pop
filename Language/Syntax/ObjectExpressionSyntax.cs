using System.Collections.Generic;

namespace Pop.Language;

public sealed class ObjectExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<ObjectPropertySyntax> Properties { get; }
    public SyntaxToken CloseBraceToken { get; }

    public ObjectExpressionSyntax(
        SyntaxToken openBraceToken,
        IReadOnlyList<ObjectPropertySyntax> properties,
        SyntaxToken closeBraceToken)
    {
        OpenBraceToken = openBraceToken;
        Properties = properties;
        CloseBraceToken = closeBraceToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ObjectExpression;
    public override TextSpan Span => TextSpan.FromBounds(OpenBraceToken.Span.Start, CloseBraceToken.Span.End);
}
