using System.IO;
using Pop.Language;
using Pop.Runtime;

namespace Tests;

[TestClass]
public sealed class RuntimeTests
{
    [TestMethod]
    public void ExecuteText_WritesBuiltInPrintOutput()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("print(\"hello\")");

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public void ExecuteText_WritesCornBuiltInPrintOutput()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("corn.print(\"hello\")");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_WritesCornBuiltInPrintLnOutput()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("corn.println(\"hello\")");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesFunctionsAndVariables()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            fun add(a, b) {
                ret a + b
            }

            var total -> add(2, 3)
            corn.println(total)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("5\r\n", writer.ToString());
    }

    [TestMethod]
    public void Execute_LoadsInjectedFileImmediately()
    {
        var directory = Directory.CreateTempSubdirectory();
        var injectedPath = Path.Combine(directory.FullName, "injected.pop");
        File.WriteAllText(injectedPath, "public fun value() { ret \"injected\" }");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);
        var source = SourceFile.FromText($"var module -> inject \"{injectedPath.Replace("\\", "\\\\")}\"\ncorn.println(module.value())");

        var result = runtime.Execute(source);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("injected\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_HandlesLoopControlFlow()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> 0
            while true {
                value -> value + 1

                if value == 2 {
                    cont
                }

                corn.println(value)

                if value == 3 {
                    abort
                }
            }
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("1\r\n3\r\n", writer.ToString());
    }

    [TestMethod]
    public void Execute_ExposesOnlyPublicInjectedMembers()
    {
        var directory = Directory.CreateTempSubdirectory();
        var injectedPath = Path.Combine(directory.FullName, "math.pop");
        File.WriteAllText(injectedPath, """
            public fun add(a, b) {
                ret a + b
            }

            fun hidden() {
                ret 0
            }
            """);

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var math -> inject "{{injectedPath.Replace("\\", "\\\\")}}"
            corn.println(math.add(10, 10))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("20\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesTypeAndConversionBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            corn.println(corn.type(1))
            corn.println(corn.str(12))
            corn.println(corn.int("42"))
            corn.println(corn.double("32.5"))
            corn.println(corn.bool(""))
            corn.println(corn.bool("x"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("int\r\n12\r\n42\r\n32.5\r\nfalse\r\ntrue\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesCornBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            corn.println(corn.type(1))
            corn.println(corn.type(nil))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("int\r\nnil\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesNilLiteral()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> nil
            corn.println(value)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("null\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectAndArrayBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" age: 32 }
            corn.println(corn.keys(obj).len)
            corn.println(corn.has(obj, "name"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("2\r\ntrue\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectGet()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" age: 32 }
            corn.println(obj.get("name"))
            corn.println(obj.get("age"))
            corn.println(obj.get("missing"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("bob\r\n32\r\nnull\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesStringAndObjectLenMembers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "hello"
            var obj -> { name: "bob" age: 32 }
            corn.println(text.len)
            corn.println(obj.len)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("5\r\n2\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesStringAddAndRemoveMembers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "hello"
            text -> text.add("!")
            corn.println(text)
            text -> text.remove(1)
            corn.println(text)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello!\r\nhllo!\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesStringReplaceMember()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "hello"
            text -> text.replace(1, "a")
            corn.println(text)
            text -> text.replace(10, "z")
            corn.println(text)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hallo\r\nhallo\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesStringAtMember()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "hello"
            corn.println(text.at(1))
            corn.println(text.at(10))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("e\r\nnull\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesStringForEach()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "hey"
            text.forEach(@(character) {
                corn.println(character)
            })
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("h\r\ne\r\ny\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_PrefersExplicitObjectLenProperty()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { len: 99 name: "bob" }
            corn.println(obj.len)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("99\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectAddAndRemoveMembers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" }
            obj.add("age", 32)
            corn.println(obj.len)
            corn.println(obj.get("age"))
            corn.println(obj.remove("name"))
            corn.println(obj.len)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("2\r\n32\r\nbob\r\n1\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectMemberAssignment()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" }
            obj.name -> 1
            obj.age -> 32
            corn.println(obj.get("name"))
            corn.println(obj.get("age"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("1\r\n32\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectForEach()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" age: 32 }
            obj.forEach(@(key, value) {
                corn.println(key)
                corn.println(value)
            })
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("name\r\nbob\r\nage\r\n32\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesArrayMemberLenAndAt()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 20, 30]
            corn.println(arr.len)
            corn.println(arr.at(1))
            corn.println(arr.at(10))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("3\r\n20\r\nnull\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesArrayAddAndRemoveMembers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 20]
            arr.add(30)
            corn.println(arr.len)
            corn.println(arr.remove(1))
            corn.println(arr.len)
            corn.println(arr.at(1))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("3\r\n20\r\n2\r\n30\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesArrayReplaceMember()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 20, 30]
            corn.println(arr.replace(1, 99))
            corn.println(arr.at(1))
            corn.println(arr.replace(10, 0))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("20\r\n99\r\nnull\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesArrayForEach()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 20, 30]
            arr.forEach(@(elem) {
                corn.println(elem)
            })
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("10\r\n20\r\n30\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesInputBuiltIn()
    {
        var writer = new StringWriter();
        var input = new StringReader("hello from stdin\r\n");
        var runtime = new ScriptRuntime(writer, input);

        var result = runtime.ExecuteText("""
            var line -> corn.input()
            corn.println(line)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello from stdin\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_ReadsAndWritesFiles()
    {
        var directory = Directory.CreateTempSubdirectory();
        var filePath = Path.Combine(directory.FullName, "data.txt");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            corn.write("{{filePath.Replace("\\", "\\\\")}}", "hello file")
            corn.println(corn.read("{{filePath.Replace("\\", "\\\\")}}"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello file\r\n", writer.ToString());
    }
}
