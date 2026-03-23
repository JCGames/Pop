using Pop.Language;

namespace Tests;

[TestClass]
public sealed class SourceFileTests
{
    [TestMethod]
    public void GetLocation_AtEndOfFileAfterTrailingLf_UsesFinalEmptyLine()
    {
        var sourceFile = SourceFile.FromText("alpha\n");

        var location = sourceFile.GetLocation(sourceFile.Length);

        Assert.AreEqual(2, location.Line);
        Assert.AreEqual(1, location.Column);
    }

    [TestMethod]
    public void GetLocation_AtEndOfFileAfterTrailingCrLf_UsesFinalEmptyLine()
    {
        var sourceFile = SourceFile.FromText("alpha\r\n");

        var location = sourceFile.GetLocation(sourceFile.Length);

        Assert.AreEqual(2, location.Line);
        Assert.AreEqual(1, location.Column);
    }

    [TestMethod]
    public void GetLineText_ForTrailingEmptyLine_ReturnsEmptyString()
    {
        var sourceFile = SourceFile.FromText("alpha\n");

        var lineText = sourceFile.GetLineText(2);

        Assert.AreEqual(string.Empty, lineText);
    }

    [TestMethod]
    public void Slice_WithImpossibleSpan_ThrowsArgumentOutOfRangeException()
    {
        var sourceFile = SourceFile.FromText("alpha");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sourceFile.Slice(4, int.MaxValue));
    }
}
