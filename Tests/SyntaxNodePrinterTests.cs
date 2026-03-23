using System.IO;
using Pop.Language;

namespace Tests;

[TestClass]
public sealed class SyntaxNodePrinterTests
{
    [TestMethod]
    public void WriteTo_PrintsExpressionTree()
    {
        var result = Parser.ParseText("value -> 1 + 2 * 3");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── AssignmentExpression" + "\r\n" +
            "            ├── NameExpression value" + "\r\n" +
            "            └── BinaryExpression" + "\r\n" +
            "                ├── LiteralExpression 1" + "\r\n" +
            "                └── BinaryExpression" + "\r\n" +
            "                    ├── LiteralExpression 2" + "\r\n" +
            "                    └── LiteralExpression 3" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsVariableDeclarationTree()
    {
        var result = Parser.ParseText("var value -> 1 + 2");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── VariableDeclarationExpression value" + "\r\n" +
            "            ├── NameExpression value" + "\r\n" +
            "            └── BinaryExpression" + "\r\n" +
            "                ├── LiteralExpression 1" + "\r\n" +
            "                └── LiteralExpression 2" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsCallExpressionTree()
    {
        var result = Parser.ParseText("add(left, right)");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── CallExpression" + "\r\n" +
            "            ├── NameExpression add" + "\r\n" +
            "            ├── NameExpression left" + "\r\n" +
            "            └── NameExpression right" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsLambdaExpressionTree()
    {
        var result = Parser.ParseText("var lambda -> @(left, right) { ret left + right }");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── VariableDeclarationExpression lambda" + "\r\n" +
            "            ├── NameExpression lambda" + "\r\n" +
            "            └── LambdaExpression" + "\r\n" +
            "                ├── Parameter" + "\r\n" +
            "                │   └── NameExpression left" + "\r\n" +
            "                ├── Parameter" + "\r\n" +
            "                │   └── NameExpression right" + "\r\n" +
            "                └── BlockStatement" + "\r\n" +
            "                    └── ReturnStatement" + "\r\n" +
            "                        └── BinaryExpression" + "\r\n" +
            "                            ├── NameExpression left" + "\r\n" +
            "                            └── NameExpression right" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsMemberAccessTree()
    {
        var result = Parser.ParseText("obj.name");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── MemberAccessExpression name" + "\r\n" +
            "            └── NameExpression obj" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsObjectLiteralTree()
    {
        var result = Parser.ParseText("var obj -> { name: \"bob\" location: \"france\" age: 32 }");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── VariableDeclarationExpression obj" + "\r\n" +
            "            ├── NameExpression obj" + "\r\n" +
            "            └── ObjectExpression" + "\r\n" +
            "                ├── ObjectProperty name" + "\r\n" +
            "                │   └── LiteralExpression \"bob\"" + "\r\n" +
            "                ├── ObjectProperty location" + "\r\n" +
            "                │   └── LiteralExpression \"france\"" + "\r\n" +
            "                └── ObjectProperty age" + "\r\n" +
            "                    └── LiteralExpression 32" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsArrayLiteralTree()
    {
        var result = Parser.ParseText("var arr -> [elem1, 32, \"hello\"]");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── VariableDeclarationExpression arr" + "\r\n" +
            "            ├── NameExpression arr" + "\r\n" +
            "            └── ArrayExpression" + "\r\n" +
            "                ├── NameExpression elem1" + "\r\n" +
            "                ├── LiteralExpression 32" + "\r\n" +
            "                └── LiteralExpression \"hello\"" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsChainedAssignmentTree()
    {
        var result = Parser.ParseText("a -> b -> c");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── AssignmentExpression" + "\r\n" +
            "            ├── NameExpression a" + "\r\n" +
            "            └── AssignmentExpression" + "\r\n" +
            "                ├── NameExpression b" + "\r\n" +
            "                └── NameExpression c" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsWhileStatementTree()
    {
        var result = Parser.ParseText("while flag { var value -> 1 }");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── WhileStatement" + "\r\n" +
            "        ├── NameExpression flag" + "\r\n" +
            "        └── BlockStatement" + "\r\n" +
            "            └── ExpressionStatement" + "\r\n" +
            "                └── VariableDeclarationExpression value" + "\r\n" +
            "                    ├── NameExpression value" + "\r\n" +
            "                    └── LiteralExpression 1" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsIfElseIfTree()
    {
        var result = Parser.ParseText("if first { value -> 1 } else if second { value -> 2 }");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── IfStatement" + "\r\n" +
            "        ├── NameExpression first" + "\r\n" +
            "        ├── BlockStatement" + "\r\n" +
            "        │   └── ExpressionStatement" + "\r\n" +
            "        │       └── AssignmentExpression" + "\r\n" +
            "        │           ├── NameExpression value" + "\r\n" +
            "        │           └── LiteralExpression 1" + "\r\n" +
            "        └── ElseClause" + "\r\n" +
            "            └── IfStatement" + "\r\n" +
            "                ├── NameExpression second" + "\r\n" +
            "                └── BlockStatement" + "\r\n" +
            "                    └── ExpressionStatement" + "\r\n" +
            "                        └── AssignmentExpression" + "\r\n" +
            "                            ├── NameExpression value" + "\r\n" +
            "                            └── LiteralExpression 2" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsFunctionDeclarationTree()
    {
        var result = Parser.ParseText("fun add(left, right) { value -> left + right }");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── FunctionDeclarationStatement add" + "\r\n" +
            "        ├── Parameter" + "\r\n" +
            "        │   └── NameExpression left" + "\r\n" +
            "        ├── Parameter" + "\r\n" +
            "        │   └── NameExpression right" + "\r\n" +
            "        └── BlockStatement" + "\r\n" +
            "            └── ExpressionStatement" + "\r\n" +
            "                └── AssignmentExpression" + "\r\n" +
            "                    ├── NameExpression value" + "\r\n" +
            "                    └── BinaryExpression" + "\r\n" +
            "                        ├── NameExpression left" + "\r\n" +
            "                        └── NameExpression right" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsReturnStatementTree()
    {
        var result = Parser.ParseText("ret left + right");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ReturnStatement" + "\r\n" +
            "        └── BinaryExpression" + "\r\n" +
            "            ├── NameExpression left" + "\r\n" +
            "            └── NameExpression right" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsContinueStatementTree()
    {
        var result = Parser.ParseText("cont");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ContinueStatement" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsBreakStatementTree()
    {
        var result = Parser.ParseText("abort");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── BreakStatement" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void WriteTo_PrintsInjectExpressionTree()
    {
        var result = Parser.ParseText("inject \"my file path\"");
        var writer = new StringWriter();

        SyntaxNodePrinter.WriteTo(writer, result.Root);

        const string expected =
            "└── CompilationUnit" + "\r\n" +
            "    └── ExpressionStatement" + "\r\n" +
            "        └── InjectExpression \"my file path\"" + "\r\n";

        Assert.AreEqual(expected, writer.ToString());
    }
}
