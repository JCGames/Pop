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
    public void CreateText_InfersNilVariableType()
    {
        var model = SemanticModel.CreateText("var value -> nil");

        Assert.IsEmpty(model.Diagnostics);
        var variable = model.GlobalVariables.Single(variable => variable.Name == "value");
        Assert.AreEqual(TypeSymbol.Nil, variable.Type);
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
    public void CreateText_AllowsMemberAssignmentOnObjects()
    {
        var model = SemanticModel.CreateText("var obj -> { name: \"bob\" }\nobj.name -> 1");

        Assert.IsEmpty(model.Diagnostics);
    }

    [TestMethod]
    public void CreateText_AllowsAssigningNilToString()
    {
        var model = SemanticModel.CreateText("var value -> \"hello\"\nvalue -> nil");

        Assert.IsEmpty(model.Diagnostics);
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
    public void CreateText_BindsStringLenMember()
    {
        var model = SemanticModel.CreateText("var text -> \"hello\"\ntext.len");

        Assert.IsEmpty(model.Diagnostics);
        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        var lenAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.AreEqual(TypeSymbol.Int, lenAccess.Type);
    }

    [TestMethod]
    public void CreateText_BindsStringMutationMembers()
    {
        var model = SemanticModel.CreateText("var text -> \"hello\"\ntext.add\ntext.contains\ntext.insert\ntext.replace\ntext.remove");

        Assert.IsEmpty(model.Diagnostics);

        var addStatement = (BoundExpressionStatement)model.Root.Statements[1];
        var addAccess = (BoundMemberAccessExpression)addStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(addAccess.Type);

        var addType = (FunctionTypeSymbol)addAccess.Type;
        Assert.AreEqual(TypeSymbol.String, addType.ReturnType);

        var containsStatement = (BoundExpressionStatement)model.Root.Statements[2];
        var containsAccess = (BoundMemberAccessExpression)containsStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(containsAccess.Type);

        var containsType = (FunctionTypeSymbol)containsAccess.Type;
        Assert.AreEqual(TypeSymbol.Bool, containsType.ReturnType);

        var insertStatement = (BoundExpressionStatement)model.Root.Statements[3];
        var insertAccess = (BoundMemberAccessExpression)insertStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(insertAccess.Type);

        var insertType = (FunctionTypeSymbol)insertAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, insertType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.String, insertType.ReturnType);

        var replaceStatement = (BoundExpressionStatement)model.Root.Statements[4];
        var replaceAccess = (BoundMemberAccessExpression)replaceStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(replaceAccess.Type);

        var replaceType = (FunctionTypeSymbol)replaceAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, replaceType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.String, replaceType.ReturnType);

        var removeStatement = (BoundExpressionStatement)model.Root.Statements[5];
        var removeAccess = (BoundMemberAccessExpression)removeStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(removeAccess.Type);

        var removeType = (FunctionTypeSymbol)removeAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, removeType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.String, removeType.ReturnType);
    }

    [TestMethod]
    public void CreateText_BindsStringAtMember()
    {
        var model = SemanticModel.CreateText("var text -> \"hello\"\ntext.at");

        Assert.IsEmpty(model.Diagnostics);

        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        var atAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(atAccess.Type);

        var atType = (FunctionTypeSymbol)atAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, atType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Char, atType.ReturnType);
    }

    [TestMethod]
    public void CreateText_BindsStringForEachMember()
    {
        var model = SemanticModel.CreateText("var text -> \"hello\"\ntext.forEach");

        Assert.IsEmpty(model.Diagnostics);

        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        var forEachAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(forEachAccess.Type);

        var forEachType = (FunctionTypeSymbol)forEachAccess.Type;
        Assert.AreEqual(TypeSymbol.Void, forEachType.ReturnType);
        Assert.IsInstanceOfType<FunctionTypeSymbol>(forEachType.ParameterTypes[0]);

        var callbackType = (FunctionTypeSymbol)forEachType.ParameterTypes[0];
        Assert.AreEqual(TypeSymbol.Char, callbackType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Void, callbackType.ReturnType);
    }

    [TestMethod]
    public void CreateText_BindsObjectLenMember()
    {
        var model = SemanticModel.CreateText("var obj -> { name: \"bob\" age: 32 }\nobj.len");

        Assert.IsEmpty(model.Diagnostics);
        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        var lenAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.AreEqual(TypeSymbol.Int, lenAccess.Type);
    }

    [TestMethod]
    public void CreateText_BindsObjectMutationMembers()
    {
        var model = SemanticModel.CreateText("var obj -> { name: \"bob\" age: 32 }\nobj.add\nobj.remove");

        Assert.IsEmpty(model.Diagnostics);

        var addStatement = (BoundExpressionStatement)model.Root.Statements[1];
        var addAccess = (BoundMemberAccessExpression)addStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(addAccess.Type);

        var addType = (FunctionTypeSymbol)addAccess.Type;
        Assert.AreEqual(TypeSymbol.String, addType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Void, addType.ReturnType);

        var removeStatement = (BoundExpressionStatement)model.Root.Statements[2];
        var removeAccess = (BoundMemberAccessExpression)removeStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(removeAccess.Type);

        var removeType = (FunctionTypeSymbol)removeAccess.Type;
        Assert.AreEqual(TypeSymbol.String, removeType.ParameterTypes[0]);
    }

    [TestMethod]
    public void CreateText_BindsObjectForEachMember()
    {
        var model = SemanticModel.CreateText("var obj -> { name: \"bob\" age: 32 }\nobj.forEach");

        Assert.IsEmpty(model.Diagnostics);

        var statement = (BoundExpressionStatement)model.Root.Statements[1];
        var forEachAccess = (BoundMemberAccessExpression)statement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(forEachAccess.Type);

        var forEachType = (FunctionTypeSymbol)forEachAccess.Type;
        Assert.AreEqual(TypeSymbol.Void, forEachType.ReturnType);
        Assert.IsInstanceOfType<FunctionTypeSymbol>(forEachType.ParameterTypes[0]);

        var callbackType = (FunctionTypeSymbol)forEachType.ParameterTypes[0];
        Assert.AreEqual(TypeSymbol.String, callbackType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Void, callbackType.ReturnType);
    }

    [TestMethod]
    public void CreateText_BindsArrayMutationMembers()
    {
        var model = SemanticModel.CreateText("var arr -> [1, 2, 3]\narr.add\narr.insert\narr.replace\narr.remove");

        Assert.IsEmpty(model.Diagnostics);

        var addStatement = (BoundExpressionStatement)model.Root.Statements[1];
        var addAccess = (BoundMemberAccessExpression)addStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(addAccess.Type);

        var addType = (FunctionTypeSymbol)addAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, addType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Void, addType.ReturnType);

        var insertStatement = (BoundExpressionStatement)model.Root.Statements[2];
        var insertAccess = (BoundMemberAccessExpression)insertStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(insertAccess.Type);

        var insertType = (FunctionTypeSymbol)insertAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, insertType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Void, insertType.ReturnType);

        var replaceStatement = (BoundExpressionStatement)model.Root.Statements[3];
        var replaceAccess = (BoundMemberAccessExpression)replaceStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(replaceAccess.Type);

        var replaceType = (FunctionTypeSymbol)replaceAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, replaceType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Int, replaceType.ReturnType);

        var removeStatement = (BoundExpressionStatement)model.Root.Statements[4];
        var removeAccess = (BoundMemberAccessExpression)removeStatement.Expression;
        Assert.IsInstanceOfType<FunctionTypeSymbol>(removeAccess.Type);

        var removeType = (FunctionTypeSymbol)removeAccess.Type;
        Assert.AreEqual(TypeSymbol.Int, removeType.ParameterTypes[0]);
        Assert.AreEqual(TypeSymbol.Int, removeType.ReturnType);
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
