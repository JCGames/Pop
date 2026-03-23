using System.IO;

namespace Pop.Language;

public static class SyntaxNodePrinter
{
    public static void WriteTo(TextWriter writer, SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(node);

        WriteNode(writer, node, string.Empty, isLast: true);
    }

    private static void WriteNode(TextWriter writer, SyntaxNode node, string indent, bool isLast)
    {
        writer.Write(indent);
        writer.Write(isLast ? "└── " : "├── ");
        writer.Write(node.Kind);

        switch (node)
        {
            case CompilationUnitSyntax:
            case ExpressionStatementSyntax:
            case BlockStatementSyntax:
            case WhileStatementSyntax:
            case IfStatementSyntax:
            case ElseClauseSyntax:
            case ReturnStatementSyntax:
            case ContinueStatementSyntax:
            case BreakStatementSyntax:
                break;
            case InjectExpressionSyntax inject:
                writer.Write(" ");
                writer.Write(FormatValue(inject.PathToken.Value ?? inject.PathToken.Text));
                break;
            case MemberAccessExpressionSyntax memberAccess:
                writer.Write(" ");
                writer.Write(memberAccess.IdentifierToken.Text);
                break;
            case ObjectPropertySyntax property:
                writer.Write(" ");
                writer.Write(property.IdentifierToken.Text);
                break;
            case FunctionDeclarationStatementSyntax function:
                writer.Write(" ");
                writer.Write(function.IdentifierToken.Text);
                break;
            case LiteralExpressionSyntax literal when literal.Value is not null:
                writer.Write(" ");
                writer.Write(FormatValue(literal.Value));
                break;
            case VariableDeclarationExpressionSyntax declaration:
                writer.Write(" ");
                writer.Write(declaration.IdentifierToken.Text);
                break;
            case NameExpressionSyntax name:
                writer.Write(" ");
                writer.Write(name.IdentifierToken.Text);
                break;
        }

        writer.WriteLine();

        var childIndent = indent + (isLast ? "    " : "│   ");
        var children = GetChildren(node);
        for (var i = 0; i < children.Count; i++)
        {
            WriteNode(writer, children[i], childIndent, i == children.Count - 1);
        }
    }

    private static IReadOnlyList<SyntaxNode> GetChildren(SyntaxNode node)
    {
        return node switch
        {
            CompilationUnitSyntax compilationUnit => [.. compilationUnit.Statements],
            ExpressionStatementSyntax expressionStatement => [expressionStatement.Expression],
            BlockStatementSyntax block => [.. block.Statements],
            FunctionDeclarationStatementSyntax function => [.. function.Parameters, function.Body],
            WhileStatementSyntax whileStatement => [whileStatement.Condition, whileStatement.Body],
            IfStatementSyntax ifStatement => ifStatement.ElseClause is null
                ? [ifStatement.Condition, ifStatement.ThenStatement]
                : [ifStatement.Condition, ifStatement.ThenStatement, ifStatement.ElseClause],
            ElseClauseSyntax elseClause => [elseClause.Statement],
            ReturnStatementSyntax returnStatement => returnStatement.Expression is null ? [] : [returnStatement.Expression],
            ContinueStatementSyntax => [],
            BreakStatementSyntax => [],
            InjectExpressionSyntax => [],
            ArrayExpressionSyntax arrayExpression => [.. arrayExpression.Elements],
            ObjectExpressionSyntax objectExpression => [.. objectExpression.Properties],
            ObjectPropertySyntax objectProperty => [objectProperty.Value],
            CallExpressionSyntax callExpression => [callExpression.Target, .. callExpression.Arguments],
            MemberAccessExpressionSyntax memberAccess => [memberAccess.Target],
            LambdaExpressionSyntax lambda => [.. lambda.Parameters, lambda.Body],
            ParameterSyntax parameter => [new NameExpressionSyntax(parameter.IdentifierToken)],
            ParenthesizedExpressionSyntax parenthesized => [parenthesized.Expression],
            UnaryExpressionSyntax unary => [unary.Operand],
            BinaryExpressionSyntax binary => [binary.Left, binary.Right],
            ConditionalExpressionSyntax conditional => [conditional.Condition, conditional.WhenTrue, conditional.WhenFalse],
            VariableDeclarationExpressionSyntax declaration => [new NameExpressionSyntax(declaration.IdentifierToken), declaration.Initializer],
            AssignmentExpressionSyntax assignment => [assignment.Target, assignment.Expression],
            _ => []
        };
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            string text => "\"" + text + "\"",
            char character => "'" + character + "'",
            bool boolean => boolean ? "true" : "false",
            _ => value.ToString() ?? string.Empty
        };
    }
}
