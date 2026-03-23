using Pop.Language;

namespace Tests;

[TestClass]
public sealed class SampleScriptTests
{
    [TestMethod]
    public void TestScript_ParsesWithoutDiagnostics()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(repositoryRoot, "Pop", "Scripts", "test.pop");

        var result = Parser.Parse(SourceFile.Load(new FileInfo(scriptPath)));

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsNotNull(result.Root);
    }
}
