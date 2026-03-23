namespace Pop.Language;

public sealed class SemanticModel
{
    public ParseResult ParseResult { get; }
    public BoundCompilationUnit Root { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<VariableSymbol> GlobalVariables { get; }
    public IReadOnlyList<FunctionSymbol> GlobalFunctions { get; }
    public IReadOnlyList<VariableSymbol> PublicVariables { get; }
    public IReadOnlyList<FunctionSymbol> PublicFunctions { get; }

    private SemanticModel(
        ParseResult parseResult,
        BoundCompilationUnit root,
        IReadOnlyList<Diagnostic> diagnostics,
        IReadOnlyList<VariableSymbol> globalVariables,
        IReadOnlyList<FunctionSymbol> globalFunctions,
        IReadOnlyList<VariableSymbol> publicVariables,
        IReadOnlyList<FunctionSymbol> publicFunctions)
    {
        ParseResult = parseResult;
        Root = root;
        Diagnostics = diagnostics;
        GlobalVariables = globalVariables;
        GlobalFunctions = globalFunctions;
        PublicVariables = publicVariables;
        PublicFunctions = publicFunctions;
    }

    public static SemanticModel Create(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        var binder = new Binder(parseResult);
        var root = binder.BindCompilationUnit();
        var diagnostics = parseResult.Diagnostics.Concat(binder.Diagnostics).ToArray();

        return new SemanticModel(
            parseResult,
            root,
            diagnostics,
            binder.GlobalVariables,
            binder.GlobalFunctions,
            binder.PublicVariables,
            binder.PublicFunctions);
    }

    public static SemanticModel Create(SourceFile sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
        return Create(Parser.Parse(sourceFile));
    }

    public static SemanticModel CreateText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Create(Parser.ParseText(text));
    }
}
