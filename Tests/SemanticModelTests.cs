using Pop.Language;

namespace Tests;

[TestClass]
public sealed class SemanticModelTests
{
    [TestMethod]
    public void CreateText_InfersGlobalVariableType()
    {
        var model = SemanticModel.CreateText("var value -> 1 + 2");

        Assert.IsEmpty(model.Diagnostics);
        Assert.IsTrue(model.GlobalVariables.Any(variable => variable.Name == "value"));

        var variable = model.GlobalVariables.Single(variable => variable.Name == "value");
        Assert.AreEqual(TypeSymbol.Int, variable.Type);
    }

    [TestMethod]
    public void CreateText_BindsCornBuiltInModule()
    {
        var model = SemanticModel.CreateText("corn.print");

        Assert.IsEmpty(model.Diagnostics);
        Assert.IsTrue(model.GlobalVariables.Any(variable => variable.Name == "corn"));

        var statement = (BoundExpressionStatement)model.Root.Statements[0];
        var memberAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(memberAccess.Type);
    }

    [TestMethod]
    public void CreateText_ResolvesObjectMemberAccessType()
    {
        var model = SemanticModel.CreateText("var obj -> { age: 32 }\nobj.age");

        Assert.IsEmpty(model.Diagnostics);
        Assert.HasCount(2, model.Root.Statements);
        Assert.IsInstanceOfType<BoundExpressionStatement>(model.Root.Statements[1]);

        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        Assert.IsInstanceOfType<BoundMemberAccessExpression>(statement.Expression);

        var memberAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.AreEqual(TypeSymbol.Int, memberAccess.Type);
    }

    [TestMethod]
    public void CreateText_ReportsUndefinedName()
    {
        var model = SemanticModel.CreateText("value");

        Assert.IsNotEmpty(model.Diagnostics);
        StringAssert.Contains(model.Diagnostics[0].Message, "Undefined name 'value'");
    }

    [TestMethod]
    public void CreateText_ReportsUndefinedRootBuiltInName()
    {
        var model = SemanticModel.CreateText("print(\"hello\")");

        Assert.IsNotEmpty(model.Diagnostics);
        Assert.IsTrue(model.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("Undefined name 'print'")));
    }

    [TestMethod]
    public void CreateText_ReportsAssignmentTypeMismatch()
    {
        var model = SemanticModel.CreateText("var value -> 1\nvalue -> true");

        Assert.IsNotEmpty(model.Diagnostics);
        Assert.IsTrue(model.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("Cannot assign a value of type 'bool' to 'int'.")));
    }

    [TestMethod]
    public void CreateText_BindsFunctionDeclarationAndCall()
    {
        var model = SemanticModel.CreateText("fun add(left, right) { ret left + right }\nvar total -> add(1, 2)");

        Assert.IsEmpty(model.Diagnostics);
        Assert.IsTrue(model.GlobalFunctions.Any(function => function.Name == "add"));

        var function = model.GlobalFunctions.Single(function => function.Name == "add");
        Assert.AreEqual(TypeSymbol.Any, function.ReturnType);
    }

    [TestMethod]
    public void CreateText_BindsInjectExpressionAsModuleObject()
    {
        var directory = Directory.CreateTempSubdirectory();
        var injectedPath = Path.Combine(directory.FullName, "math.pop");
        File.WriteAllText(injectedPath, "public fun add(a, b) { ret a + b }\nfun hidden() { ret 0 }");

        var model = SemanticModel.CreateText($"var math -> inject \"{injectedPath.Replace("\\", "\\\\")}\"\nmath.add");

        Assert.IsEmpty(model.Diagnostics);
        var declaration = (BoundExpressionStatement)model.Root.Statements[0];
        var module = (BoundVariableDeclarationExpression)declaration.Expression;
        Assert.IsInstanceOfType<ObjectTypeSymbol>(module.Type);

        var objectType = (ObjectTypeSymbol)module.Type;
        Assert.IsTrue(objectType.TryGetProperty("add", out _));
        Assert.IsFalse(objectType.TryGetProperty("hidden", out _));
    }

    [TestMethod]
    public void CreateText_BindsArrayBuiltInMembers()
    {
        var model = SemanticModel.CreateText("var arr -> [1, 2, 3]\narr.len\narr.at\narr.forEach");

        Assert.IsEmpty(model.Diagnostics);

        var lenStatement = (BoundExpressionStatement)model.Root.Statements[1];
        var lenAccess = (BoundMemberAccessExpression)lenStatement.Expression;
        Assert.AreEqual(TypeSymbol.Int, lenAccess.Type);

        var atStatement = (BoundExpressionStatement)model.Root.Statements[2];
        var atAccess = (BoundMemberAccessExpression)atStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(atAccess.Type);

        var atType = (FunctionTypeSymbol)atAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, atType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Int, atType.ReturnType);

        var forEachStatement = (BoundExpressionStatement)model.Root.Statements[3];
        var forEachAccess = (BoundMemberAccessExpression)forEachStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(forEachAccess.Type);

        var forEachType = (FunctionTypeSymbol)forEachAccess.Type;
        Assert.AreEqual(TypeSymbol.Void, forEachType.ReturnType);
        Assert.IsInstanceOfType<FunctionTypeSymbol>(forEachType.ParameterTypes[0]);
    }

    [TestMethod]
    public void CreateText_BindsObjectGetMember()
    {
        var model = SemanticModel.CreateText("var obj -> { name: \"bob\" age: 32 }\nobj.get");

        Assert.IsEmpty(model.Diagnostics);
        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        var getAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(getAccess.Type);

        var getType = (FunctionTypeSymbol)getAccess.Type;
        Assert.AreEqual(TypeSymbol.String, getType.ParameterTypes[0]);
    }

    [TestMethod]
    public void CreateText_ReportsContinueOutsideLoop()
    {
        var model = SemanticModel.CreateText("cont");

        Assert.IsNotEmpty(model.Diagnostics);
        Assert.IsTrue(model.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("`cont` can only be used inside a loop.")));
    }

    [TestMethod]
    public void CreateText_ReportsBreakOutsideLoop()
    {
        var model = SemanticModel.CreateText("abort");

        Assert.IsNotEmpty(model.Diagnostics);
        Assert.IsTrue(model.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("`abort` can only be used inside a loop.")));
    }

    [TestMethod]
    public void CreateText_ReportsReturnOutsideFunction()
    {
        var model = SemanticModel.CreateText("ret 1");

        Assert.IsNotEmpty(model.Diagnostics);
        Assert.IsTrue(model.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("`ret` can only be used inside a function or lambda.")));
    }
}
