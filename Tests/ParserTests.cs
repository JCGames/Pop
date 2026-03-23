using Pop.Language;

namespace Tests;

[TestClass]
public sealed class ParserTests
{
    [TestMethod]
    public void ParseText_UsesCStyleOperatorPrecedence()
    {
        var result = Parser.ParseText("1 + 2 * 3");

        Assert.IsEmpty(result.Diagnostics);
        Assert.HasCount(1, result.Root.Statements);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[0]);

        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var add = (BinaryExpressionSyntax)statement.Expression;
        Assert.AreEqual(SyntaxKind.PlusToken, add.OperatorToken.Kind);
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(add.Left);
        Assert.IsInstanceOfType<BinaryExpressionSyntax>(add.Right);

        var multiply = (BinaryExpressionSyntax)add.Right;
        Assert.AreEqual(SyntaxKind.StarToken, multiply.OperatorToken.Kind);
    }

    [TestMethod]
    public void ParseText_ParsesParenthesizedExpressions()
    {
        var result = Parser.ParseText("(1 + 2) * 3");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var multiply = (BinaryExpressionSyntax)statement.Expression;
        Assert.AreEqual(SyntaxKind.StarToken, multiply.OperatorToken.Kind);
        Assert.IsInstanceOfType<ParenthesizedExpressionSyntax>(multiply.Left);
    }

    [TestMethod]
    public void ParseText_ParsesConditionalExpressions()
    {
        var result = Parser.ParseText("flag ? 1 : 2 + 3");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<ConditionalExpressionSyntax>(statement.Expression);

        var conditional = (ConditionalExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<NameExpressionSyntax>(conditional.Condition);
        Assert.IsInstanceOfType<BinaryExpressionSyntax>(conditional.WhenFalse);
    }

    [TestMethod]
    public void ParseText_ParsesCallExpressions()
    {
        var result = Parser.ParseText("add(a, b)");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<CallExpressionSyntax>(statement.Expression);

        var call = (CallExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<NameExpressionSyntax>(call.Target);
        Assert.HasCount(2, call.Arguments);
        Assert.IsInstanceOfType<NameExpressionSyntax>(call.Arguments[0]);
        Assert.IsInstanceOfType<NameExpressionSyntax>(call.Arguments[1]);
    }

    [TestMethod]
    public void ParseText_ParsesLambdaExpressions()
    {
        var result = Parser.ParseText("var lambda -> @(param1, param2) { ret param1 + param2 }");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<LambdaExpressionSyntax>(declaration.Initializer);

        var lambda = (LambdaExpressionSyntax)declaration.Initializer;
        Assert.HasCount(2, lambda.Parameters);
        Assert.AreEqual("param1", lambda.Parameters[0].IdentifierToken.Text);
        Assert.AreEqual("param2", lambda.Parameters[1].IdentifierToken.Text);
        Assert.HasCount(1, lambda.Body.Statements);
        Assert.IsInstanceOfType<ReturnStatementSyntax>(lambda.Body.Statements[0]);
    }

    [TestMethod]
    public void ParseText_ParsesMemberAccessExpressions()
    {
        var result = Parser.ParseText("obj.name");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<MemberAccessExpressionSyntax>(statement.Expression);

        var memberAccess = (MemberAccessExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<NameExpressionSyntax>(memberAccess.Target);
        Assert.AreEqual("name", memberAccess.IdentifierToken.Text);
    }

    [TestMethod]
    public void ParseText_ParsesAssignmentExpressions()
    {
        var result = Parser.ParseText("value -> 1 + 2");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<AssignmentExpressionSyntax>(statement.Expression);

        var assignment = (AssignmentExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<NameExpressionSyntax>(assignment.Target);
        Assert.IsInstanceOfType<BinaryExpressionSyntax>(assignment.Expression);
    }

    [TestMethod]
    public void ParseText_ParsesMemberAssignmentExpressions()
    {
        var result = Parser.ParseText("obj.name -> 1");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<AssignmentExpressionSyntax>(statement.Expression);

        var assignment = (AssignmentExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<MemberAccessExpressionSyntax>(assignment.Target);
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(assignment.Expression);
    }

    [TestMethod]
    public void ParseText_ParsesVariableDeclarationExpressions()
    {
        var result = Parser.ParseText("var a -> 32.5");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<VariableDeclarationExpressionSyntax>(statement.Expression);

        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.AreEqual("a", declaration.IdentifierToken.Text);
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(declaration.Initializer);

        var literal = (LiteralExpressionSyntax)declaration.Initializer;
        Assert.AreEqual(32.5d, literal.Value);
    }

    [TestMethod]
    public void ParseText_ParsesNilLiteralExpression()
    {
        var result = Parser.ParseText("var a -> nil");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(declaration.Initializer);

        var literal = (LiteralExpressionSyntax)declaration.Initializer;
        Assert.IsNull(literal.Value);
    }

    [TestMethod]
    public void ParseText_ParsesPublicVariableDeclarationExpressions()
    {
        var result = Parser.ParseText("public var math -> inject \"math.pop\"");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.IsNotNull(declaration.PublicKeyword);
        Assert.IsInstanceOfType<InjectExpressionSyntax>(declaration.Initializer);
    }

    [TestMethod]
    public void ParseText_ParsesObjectLiteralExpression()
    {
        var result = Parser.ParseText("var obj -> { name: \"bob\" location: \"france\" age: 32 }");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<ObjectExpressionSyntax>(declaration.Initializer);

        var objectExpression = (ObjectExpressionSyntax)declaration.Initializer;
        Assert.HasCount(3, objectExpression.Properties);
        Assert.AreEqual("name", objectExpression.Properties[0].IdentifierToken.Text);
        Assert.AreEqual("location", objectExpression.Properties[1].IdentifierToken.Text);
        Assert.AreEqual("age", objectExpression.Properties[2].IdentifierToken.Text);
    }

    [TestMethod]
    public void ParseText_ParsesArrayLiteralExpression()
    {
        var result = Parser.ParseText("var arr -> [elem1, 32, \"hello\", { name: \"bob\" }]");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<ArrayExpressionSyntax>(declaration.Initializer);

        var arrayExpression = (ArrayExpressionSyntax)declaration.Initializer;
        Assert.HasCount(4, arrayExpression.Elements);
        Assert.IsInstanceOfType<NameExpressionSyntax>(arrayExpression.Elements[0]);
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(arrayExpression.Elements[1]);
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(arrayExpression.Elements[2]);
        Assert.IsInstanceOfType<ObjectExpressionSyntax>(arrayExpression.Elements[3]);
    }

    [TestMethod]
    public void ParseText_ParsesChainedAssignmentExpressions()
    {
        var result = Parser.ParseText("a -> b -> c");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<AssignmentExpressionSyntax>(statement.Expression);

        var outerAssignment = (AssignmentExpressionSyntax)statement.Expression;
        Assert.IsInstanceOfType<NameExpressionSyntax>(outerAssignment.Target);
        Assert.IsInstanceOfType<AssignmentExpressionSyntax>(outerAssignment.Expression);

        var innerAssignment = (AssignmentExpressionSyntax)outerAssignment.Expression;
        Assert.IsInstanceOfType<NameExpressionSyntax>(innerAssignment.Target);
        Assert.IsInstanceOfType<NameExpressionSyntax>(innerAssignment.Expression);

        var name = (NameExpressionSyntax)innerAssignment.Expression;
        Assert.AreEqual("c", name.IdentifierToken.Text);
    }

    [TestMethod]
    public void ParseText_ParsesVariableDeclarationWithChainedAssignmentInitializer()
    {
        var result = Parser.ParseText("var a -> b -> c");

        Assert.IsEmpty(result.Diagnostics);
        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<VariableDeclarationExpressionSyntax>(statement.Expression);

        var declaration = (VariableDeclarationExpressionSyntax)statement.Expression;
        Assert.AreEqual("a", declaration.IdentifierToken.Text);
        Assert.IsInstanceOfType<AssignmentExpressionSyntax>(declaration.Initializer);
    }

    [TestMethod]
    public void ParseText_ParsesWhileStatement()
    {
        var result = Parser.ParseText("while flag { value -> 1 }");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<WhileStatementSyntax>(result.Root.Statements[0]);

        var whileStatement = (WhileStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<NameExpressionSyntax>(whileStatement.Condition);
        Assert.HasCount(1, whileStatement.Body.Statements);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(whileStatement.Body.Statements[0]);
    }

    [TestMethod]
    public void ParseText_ParsesFunctionDeclaration()
    {
        var result = Parser.ParseText("fun add(left, right) { value -> left + right }");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<FunctionDeclarationStatementSyntax>(result.Root.Statements[0]);

        var function = (FunctionDeclarationStatementSyntax)result.Root.Statements[0];
        Assert.AreEqual("add", function.IdentifierToken.Text);
        Assert.HasCount(2, function.Parameters);
        Assert.AreEqual("left", function.Parameters[0].IdentifierToken.Text);
        Assert.AreEqual("right", function.Parameters[1].IdentifierToken.Text);
        Assert.HasCount(1, function.Body.Statements);
    }

    [TestMethod]
    public void ParseText_ParsesPublicFunctionDeclaration()
    {
        var result = Parser.ParseText("public fun add(left, right) { ret left + right }");

        Assert.IsEmpty(result.Diagnostics);
        var function = (FunctionDeclarationStatementSyntax)result.Root.Statements[0];
        Assert.IsNotNull(function.PublicKeyword);
        Assert.AreEqual("add", function.IdentifierToken.Text);
    }

    [TestMethod]
    public void ParseText_ParsesReturnStatementWithExpression()
    {
        var result = Parser.ParseText("ret 42");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<ReturnStatementSyntax>(result.Root.Statements[0]);

        var returnStatement = (ReturnStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(returnStatement.Expression);
    }

    [TestMethod]
    public void ParseText_ParsesBareReturnStatement()
    {
        var result = Parser.ParseText("ret");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<ReturnStatementSyntax>(result.Root.Statements[0]);

        var returnStatement = (ReturnStatementSyntax)result.Root.Statements[0];
        Assert.IsNull(returnStatement.Expression);
    }

    [TestMethod]
    public void ParseText_ParsesContinueStatement()
    {
        var result = Parser.ParseText("cont");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<ContinueStatementSyntax>(result.Root.Statements[0]);
    }

    [TestMethod]
    public void ParseText_ParsesBreakStatement()
    {
        var result = Parser.ParseText("abort");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<BreakStatementSyntax>(result.Root.Statements[0]);
    }

    [TestMethod]
    public void ParseText_ParsesContinueAndBreakInsideWhileStatement()
    {
        var result = Parser.ParseText("while flag { cont abort }");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<WhileStatementSyntax>(result.Root.Statements[0]);

        var whileStatement = (WhileStatementSyntax)result.Root.Statements[0];
        Assert.HasCount(2, whileStatement.Body.Statements);
        Assert.IsInstanceOfType<ContinueStatementSyntax>(whileStatement.Body.Statements[0]);
        Assert.IsInstanceOfType<BreakStatementSyntax>(whileStatement.Body.Statements[1]);
    }

    [TestMethod]
    public void ParseText_ParsesInjectExpressionStatement()
    {
        var result = Parser.ParseText("inject \"my file path\"");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[0]);

        var statement = (ExpressionStatementSyntax)result.Root.Statements[0];
        var injectExpression = (InjectExpressionSyntax)statement.Expression;
        Assert.AreEqual("my file path", injectExpression.PathToken.Value);
    }

    [TestMethod]
    public void ParseText_ParsesIfStatement()
    {
        var result = Parser.ParseText("if flag { value -> 1 }");

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsInstanceOfType<IfStatementSyntax>(result.Root.Statements[0]);

        var ifStatement = (IfStatementSyntax)result.Root.Statements[0];
        Assert.IsInstanceOfType<NameExpressionSyntax>(ifStatement.Condition);
        Assert.HasCount(1, ifStatement.ThenStatement.Statements);
        Assert.IsNull(ifStatement.ElseClause);
    }

    [TestMethod]
    public void ParseText_ParsesIfElseStatement()
    {
        var result = Parser.ParseText("if flag { value -> 1 } else { value -> 2 }");

        Assert.IsEmpty(result.Diagnostics);
        var ifStatement = (IfStatementSyntax)result.Root.Statements[0];
        Assert.IsNotNull(ifStatement.ElseClause);
        Assert.IsInstanceOfType<BlockStatementSyntax>(ifStatement.ElseClause.Statement);
    }

    [TestMethod]
    public void ParseText_ParsesElseIfStatement()
    {
        var result = Parser.ParseText("if first { value -> 1 } else if second { value -> 2 }");

        Assert.IsEmpty(result.Diagnostics);
        var ifStatement = (IfStatementSyntax)result.Root.Statements[0];
        Assert.IsNotNull(ifStatement.ElseClause);
        Assert.IsInstanceOfType<IfStatementSyntax>(ifStatement.ElseClause.Statement);
    }

    [TestMethod]
    public void ParseText_ReportsDiagnosticForQuestionElseSyntax()
    {
        var result = Parser.ParseText("if flag { value -> 1 } ?else { value -> 2 }");

        Assert.IsNotEmpty(result.Diagnostics);
        StringAssert.Contains(result.Diagnostics[0].Message, "Expected integer literal");
    }

    [TestMethod]
    public void ParseText_ParsesMultipleTopLevelStatements()
    {
        var result = Parser.ParseText("inject \"my file path\"\n\nfun add(a, b) { ret a + b }");

        Assert.IsEmpty(result.Diagnostics);
        Assert.HasCount(2, result.Root.Statements);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[0]);
        Assert.IsInstanceOfType<FunctionDeclarationStatementSyntax>(result.Root.Statements[1]);
    }

    [TestMethod]
    public void ParseText_IgnoresSingleLineComments()
    {
        var result = Parser.ParseText("""
            // comment before code
            var value -> 1
            // comment after declaration
            value -> value + 1
            """);

        Assert.IsEmpty(result.Diagnostics);
        Assert.HasCount(2, result.Root.Statements);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[0]);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[1]);
    }

    [TestMethod]
    public void ParseText_IgnoresTrailingSingleLineComments()
    {
        var result = Parser.ParseText("""
            var value -> 1 // initialize value
            value -> value + 1 // increment value
            """);

        Assert.IsEmpty(result.Diagnostics);
        Assert.HasCount(2, result.Root.Statements);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[0]);
        Assert.IsInstanceOfType<ExpressionStatementSyntax>(result.Root.Statements[1]);
    }

    [TestMethod]
    public void ParseText_ReportsDiagnosticForMissingOperand()
    {
        var result = Parser.ParseText("1 +");

        Assert.HasCount(1, result.Diagnostics);
        StringAssert.Contains(result.Diagnostics[0].Message, "Expected integer literal but found end of file.");
    }

    [TestMethod]
    public void ParseText_ReportsDiagnosticForLegacyAssignmentSyntax()
    {
        var result = Parser.ParseText("value = 1");

        Assert.HasCount(1, result.Diagnostics);
        StringAssert.Contains(result.Diagnostics[0].Message, "Unexpected character '='.");
    }
}
