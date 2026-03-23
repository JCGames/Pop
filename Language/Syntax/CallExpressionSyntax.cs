using System.Collections.Generic;

namespace Pop.Language;

public sealed class CallExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Target { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenToken { get; }

    public CallExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken openParenToken,
        IReadOnlyList<ExpressionSyntax> arguments,
        SyntaxToken closeParenToken)
    {
        Target = target;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CallExpression;
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, CloseParenToken.Span.End);
}
