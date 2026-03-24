using System.IO;
using System.Net;
using System.Net.Sockets;
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
                    skip
                }

                corn.println(value)

                if value == 3 {
                    break
                }
            }
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("1\r\n3\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UpdatesVariableWithPostfixIncrementAndDecrement()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> 1
            value++
            value--
            value++
            corn.println(value)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("2\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UpdatesObjectMemberWithPostfixIncrementAndDecrement()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var obj -> { count: 1 }
            obj.count++
            obj.count++
            obj.count--
            corn.println(obj.count)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("2\r\n", writer.ToString());
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
    public void ExecuteText_ReinjectsModuleWithFreshRuntimeState()
    {
        var directory = Directory.CreateTempSubdirectory();
        var injectedPath = Path.Combine(directory.FullName, "module.pop");
        File.WriteAllText(injectedPath, """
            public var values -> []
            """);

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var first -> inject "{{injectedPath.Replace("\\", "\\\\")}}"
            var second -> inject "{{injectedPath.Replace("\\", "\\\\")}}"
            first.values.add(1)
            corn.println(first.values.len)
            corn.println(second.values.len)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("1\r\n0\r\n", writer.ToString());
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
    public void ExecuteText_ReturnsErrorObjectForInvalidIntConversion()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.int("Hello")
            corn.println(corn.isError(value))
            corn.println(value.code)
            corn.println(value.message)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nint_conversion_failed\r\nint conversion failed.\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_PropagatesErrorsThroughBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.double(corn.int("Hello"))
            corn.println(corn.isError(value))
            corn.println(value.code)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nint_conversion_failed\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_ReturnsErrorObjectForMissingFileRead()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.fs.read("C:\\definitely\\missing\\file.pop")
            corn.println(corn.isError(value))
            corn.println(value.code)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nfile_read_failed\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_CreatesUserErrorObject()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.error("bad_input", "Input was invalid.")
            corn.println(corn.isError(value))
            corn.println(value.code)
            corn.println(value.message)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nbad_input\r\nInput was invalid.\r\n", writer.ToString());
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
    public void ExecuteText_EvaluatesIntMinAndMaxMembers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> 1
            corn.println(value.min)
            corn.println(value.max)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual($"{long.MinValue}\r\n{long.MaxValue}\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesDoubleMinAndMaxMembers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> 1.5
            corn.println(value.min)
            corn.println(value.max)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual($"{double.MinValue}\r\n{double.MaxValue}\r\n", writer.ToString());
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
    public void ExecuteText_EvaluatesStringInsertMember()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "helo"
            text -> text.insert(2, "l")
            corn.println(text)
            text -> text.insert(99, "!")
            corn.println(text)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello\r\nhello!\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_EvaluatesStringContainsMember()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var text -> "hello"
            corn.println(text.contains("ell"))
            corn.println(text.contains('z'))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nfalse\r\n", writer.ToString());
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
    public void ExecuteText_EvaluatesArrayInsertMember()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var arr -> [10, 30]
            arr.insert(1, 20)
            corn.println(arr.len)
            corn.println(arr.at(1))
            arr.insert(99, 40)
            corn.println(arr.at(3))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("3\r\n20\r\n40\r\n", writer.ToString());
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
            corn.fs.write("{{filePath.Replace("\\", "\\\\")}}", "hello file")
            corn.println(corn.fs.read("{{filePath.Replace("\\", "\\\\")}}"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello file\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesCornFsPathHelpers()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            corn.println(corn.fs.join("root", "file.txt"))
            corn.println(corn.fs.name("folder\\data.txt"))
            corn.println(corn.fs.stem("folder\\data.txt"))
            corn.println(corn.fs.ext("folder\\data.txt"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual($"{Path.Combine("root", "file.txt")}\r\ndata.txt\r\ndata\r\n.txt\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesCornFsMetadataHelpers()
    {
        var directory = Directory.CreateTempSubdirectory();
        var filePath = Path.Combine(directory.FullName, "data.txt");
        File.WriteAllText(filePath, "hello");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            corn.println(corn.fs.isFile("{{filePath.Replace("\\", "\\\\")}}"))
            corn.println(corn.fs.isDir("{{directory.FullName.Replace("\\", "\\\\")}}"))
            corn.println(corn.fs.size("{{filePath.Replace("\\", "\\\\")}}"))
            corn.println(corn.bool(corn.fs.created("{{filePath.Replace("\\", "\\\\")}}")))
            corn.println(corn.bool(corn.fs.modified("{{filePath.Replace("\\", "\\\\")}}")))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\ntrue\r\n5\r\ntrue\r\ntrue\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesCornFsFileOperations()
    {
        var directory = Directory.CreateTempSubdirectory();
        var sourcePath = Path.Combine(directory.FullName, "source.txt");
        var copyPath = Path.Combine(directory.FullName, "copy.txt");
        var movedPath = Path.Combine(directory.FullName, "moved.txt");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            corn.fs.write("{{sourcePath.Replace("\\", "\\\\")}}", "hello")
            corn.fs.append("{{sourcePath.Replace("\\", "\\\\")}}", " world")
            corn.fs.copy("{{sourcePath.Replace("\\", "\\\\")}}", "{{copyPath.Replace("\\", "\\\\")}}")
            corn.fs.move("{{copyPath.Replace("\\", "\\\\")}}", "{{movedPath.Replace("\\", "\\\\")}}")
            corn.println(corn.fs.read("{{sourcePath.Replace("\\", "\\\\")}}"))
            corn.println(corn.fs.exists("{{movedPath.Replace("\\", "\\\\")}}"))
            corn.fs.remove("{{movedPath.Replace("\\", "\\\\")}}")
            corn.println(corn.fs.exists("{{movedPath.Replace("\\", "\\\\")}}"))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("hello world\r\ntrue\r\nfalse\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesCornFsDirectoryOperations()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var directory = Directory.CreateTempSubdirectory();
        var nestedDirectory = Path.Combine(directory.FullName, "nested");
        var childDirectory = Path.Combine(nestedDirectory, "child");
        File.WriteAllText(Path.Combine(directory.FullName, "a.txt"), "a");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        try
        {
            var result = runtime.ExecuteText($$"""
                corn.fs.mkdir("{{nestedDirectory.Replace("\\", "\\\\")}}")
                corn.fs.mkdir("{{childDirectory.Replace("\\", "\\\\")}}")
                corn.println(corn.fs.list("{{directory.FullName.Replace("\\", "\\\\")}}").len)
                corn.println(corn.fs.files("{{directory.FullName.Replace("\\", "\\\\")}}").len)
                corn.println(corn.fs.dirs("{{directory.FullName.Replace("\\", "\\\\")}}").len)
                corn.println(corn.fs.parent("{{childDirectory.Replace("\\", "\\\\")}}"))
                corn.println(corn.fs.absolute("{{nestedDirectory.Replace("\\", "\\\\")}}"))
                corn.fs.chdir("{{nestedDirectory.Replace("\\", "\\\\")}}")
                corn.println(corn.fs.cwd())
                """);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual($"2\r\n1\r\n1\r\n{nestedDirectory}\r\n{nestedDirectory}\r\n{nestedDirectory}\r\n", writer.ToString());
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Execute_LoadsCoreMathLibraryModule()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var mathPath = Path.Combine(repositoryRoot, "Pop", "lib", "math.pop");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var math -> inject "{{mathPath.Replace("\\", "\\\\")}}"
            corn.println(math.min(10, 4))
            corn.println(math.max(10, 4))
            corn.println(math.clamp(12, 0, 5))
            corn.println(math.abs(-3))
            corn.println(math.sign(-10))
            corn.println(math.lerp(10, 20, 0.25))
            corn.println(math.sqrt(9))
            corn.println(math.pow(2, 3))
            corn.println(math.floor(3.9))
            corn.println(math.ceil(3.1))
            corn.println(math.round(3.6))
            corn.println(math.trunc(3.6))
            corn.println(math.isEven(4))
            corn.println(math.isOdd(5))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("4\r\n10\r\n5\r\n3\r\n-1\r\n12.5\r\n3\r\n8\r\n3\r\n4\r\n4\r\n3\r\ntrue\r\ntrue\r\n", writer.ToString());
    }

    [TestMethod]
    public void Execute_LoadsCoreErrorsLibraryModule()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var errorsPath = Path.Combine(repositoryRoot, "Pop", "lib", "errors.pop");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var errors -> inject "{{errorsPath.Replace("\\", "\\\\")}}"
            var value -> errors.make("bad_input", "Nope")
            corn.println(errors.is(value))
            corn.println(errors.code(value))
            corn.println(errors.message(value))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nbad_input\r\nNope\r\n", writer.ToString());
    }

    [TestMethod]
    public void Execute_LoadsCoreFsLibraryModule()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var fsPath = Path.Combine(repositoryRoot, "Pop", "lib", "fs.pop");
        var directory = Directory.CreateTempSubdirectory();
        var filePath = Path.Combine(directory.FullName, "data.txt");
        File.WriteAllText(filePath, "hello");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var fs -> inject "{{fsPath.Replace("\\", "\\\\")}}"
            corn.println(fs.join("lib", "data.txt"))
            corn.println(fs.exists("{{filePath.Replace("\\", "\\\\")}}"))
            var info -> fs.info("{{filePath.Replace("\\", "\\\\")}}")
            corn.println(info.isFile)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual($"{Path.Combine("lib", "data.txt")}\r\ntrue\r\ntrue\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesCornMathBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            corn.println(corn.math.pi)
            corn.println(corn.math.tau)
            corn.println(corn.math.e)
            corn.println(corn.math.sqrt(16))
            corn.println(corn.math.pow(2, 4))
            corn.println(corn.math.sin(0))
            corn.println(corn.math.cos(0))
            corn.println(corn.math.atan2(0, 1))
            corn.println(corn.math.log10(100))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual($"{Math.PI}\r\n{Math.Tau}\r\n{Math.E}\r\n4\r\n16\r\n0\r\n1\r\n0\r\n2\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_UsesCornJsonBuiltIns()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.json.parse("{\"name\":\"pop\",\"nums\":[1,2.5],\"active\":true,\"empty\":null}")
            corn.println(value.name)
            corn.println(value.nums.len)
            corn.println(value.nums.at(1))
            corn.println(value.active)
            corn.println(corn.type(value.empty))
            corn.println(corn.json.stringify(value))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("pop\r\n2\r\n2.5\r\ntrue\r\nnil\r\n{\"name\":\"pop\",\"nums\":[1,2.5],\"active\":true,\"empty\":null}\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_PrettyPrintsJson()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> { name: "pop" nums: [1, 2] }
            corn.println(corn.json.pretty(value))
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("{\r\n  \"name\": \"pop\",\r\n  \"nums\": [\r\n    1,\r\n    2\r\n  ]\r\n}\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_ReturnsErrorObjectForInvalidJsonParse()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.json.parse("{")
            corn.println(corn.isError(value))
            corn.println(value.code)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\njson_parse_failed\r\n", writer.ToString());
    }

    [TestMethod]
    public async Task ExecuteText_UsesCornHttpGet()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            context.Response.Headers["X-Test"] = "ok";
            await using var writer = new StreamWriter(context.Response.OutputStream, System.Text.Encoding.UTF8, 1024, leaveOpen: true);
            await writer.WriteAsync("hello from server");
            await writer.FlushAsync();
            context.Response.Close();
        });

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        try
        {
            var result = runtime.ExecuteText($$"""
                var response -> corn.http.get("http://127.0.0.1:{{port}}/")
                corn.println(response.ok)
                corn.println(response.status)
                corn.println(response.body)
                corn.println(response.headers.get("content-type"))
                corn.println(response.headers.get("x-test"))
                """);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("true\r\n200\r\nhello from server\r\ntext/plain\r\nok\r\n", writer.ToString());
            await serverTask;
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public async Task ExecuteText_UsesCornHttpRequest()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();

            context.Response.StatusCode = 201;
            context.Response.ContentType = "text/plain";
            await using var writer = new StreamWriter(context.Response.OutputStream, System.Text.Encoding.UTF8, 1024, leaveOpen: true);
            await writer.WriteAsync($"{context.Request.HttpMethod}|{body}|{context.Request.Headers["X-Test"]}|{context.Request.ContentType}");
            await writer.FlushAsync();
            context.Response.Close();
        });

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        try
        {
            var result = runtime.ExecuteText($$"""
                var headers -> {}
                headers.add("x-test", "Pop")
                headers.add("content-type", "text/plain")
                var response -> corn.http.request("POST", "http://127.0.0.1:{{port}}/", "hello", headers)
                corn.println(response.status)
                corn.println(response.method)
                corn.println(response.body)
                """);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("201\r\nPOST\r\nPOST|hello|Pop|text/plain\r\n", writer.ToString());
            await serverTask;
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public void ExecuteText_ReturnsErrorObjectForInvalidCornMathDomain()
    {
        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText("""
            var value -> corn.math.sqrt(-1)
            corn.println(corn.isError(value))
            corn.println(value.code)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\nsqrt_failed\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_ReturnsFileInfoFromCornFs()
    {
        var directory = Directory.CreateTempSubdirectory();
        var filePath = Path.Combine(directory.FullName, "data.txt");
        File.WriteAllText(filePath, "hello");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var info -> corn.fs.info("{{filePath.Replace("\\", "\\\\")}}")
            corn.println(info.exists)
            corn.println(info.isFile)
            corn.println(info.isDir)
            corn.println(info.size)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("true\r\ntrue\r\nfalse\r\n5\r\n", writer.ToString());
    }

    [TestMethod]
    public void ExecuteText_ListsDirectoryEntriesFromCornFs()
    {
        var directory = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(directory.FullName, "a.txt"), "a");
        File.WriteAllText(Path.Combine(directory.FullName, "b.txt"), "b");

        var writer = new StringWriter();
        var runtime = new ScriptRuntime(writer);

        var result = runtime.ExecuteText($$"""
            var entries -> corn.fs.list("{{directory.FullName.Replace("\\", "\\\\")}}")
            corn.println(entries.len)
            """);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("2\r\n", writer.ToString());
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
