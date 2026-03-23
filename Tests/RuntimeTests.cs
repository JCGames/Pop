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
            corn.print(total)
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
        var source = SourceFile.FromText($"var module -> inject \"{injectedPath.Replace("\\", "\\\\")}\"\ncorn.print(module.value())");

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

                corn.print(value)

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
            corn.print(math.add(10, 10))
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
            corn.print(corn.type(1))
            corn.print(corn.str(12))
            corn.print(corn.int("42"))
            corn.print(corn.double("32.5"))
            corn.print(corn.bool(""))
            corn.print(corn.bool("x"))
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
            corn.print(corn.type(1))
            corn.print(corn.len([1, 2, 3]))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("int\r\n3\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectAndArrayBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" age: 32 }
            var arr -> [1, 2]
            corn.push(arr, 3)
            corn.print(corn.len(arr))
            corn.print(corn.pop(arr))
            corn.print(corn.len(arr))
            corn.print(corn.len(corn.keys(obj)))
            corn.print(corn.has(obj, "name"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("3\r\n3\r\n2\r\n2\r\ntrue\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesObjectGet()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { name: "bob" age: 32 }
            corn.print(obj.get("name"))
            corn.print(obj.get("age"))
            corn.print(obj.get("missing"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("bob\r\n32\r\nnull\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesArrayMemberLenAndAt()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 20, 30]
            corn.print(arr.len)
            corn.print(arr.at(1))
            corn.print(arr.at(10))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("3\r\n20\r\nnull\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesArrayForEach()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 20, 30]
            arr.forEach(@(elem) {
                corn.print(elem)
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
            corn.print(line)
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
            corn.print(corn.read("{{filePath.Replace("\\", "\\\\")}}"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello file\r\n", writer.ToString());
    }
}
