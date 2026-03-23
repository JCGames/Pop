namespace Pop.Language;

public sealed class ParseResult
{
    public SourceFile SourceFile { get; }
    public CompilationUnitSyntax Root { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public ParseResult(
        SourceFile sourceFile,
        CompilationUnitSyntax root,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        SourceFile = sourceFile;
        Root = root;
        Diagnostics = diagnostics;
    }
}
