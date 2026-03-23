namespace Pop.Language;

public static class BuiltInSymbols
{
    public static FunctionSymbol Print { get; } = new(
        "print",
        [new VariableSymbol("value", TypeSymbol.Any, isParameter: true)],
        TypeSymbol.Void,
        isBuiltIn: true);
    public static FunctionSymbol PrintLn { get; } = new(
        "println",
        [new VariableSymbol("value", TypeSymbol.Any, isParameter: true)],
        TypeSymbol.Void,
        isBuiltIn: true);

    public static FunctionSymbol Type { get; } = Create("type", [Parameter("value")], TypeSymbol.String);
    public static FunctionSymbol Str { get; } = Create("str", [Parameter("value")], TypeSymbol.String);
    public static FunctionSymbol Int { get; } = Create("int", [Parameter("value")], TypeSymbol.Int);
    public static FunctionSymbol Double { get; } = Create("double", [Parameter("value")], TypeSymbol.Double);
    public static FunctionSymbol Bool { get; } = Create("bool", [Parameter("value")], TypeSymbol.Bool);
    public static FunctionSymbol Input { get; } = Create("input", [], TypeSymbol.String);
    public static FunctionSymbol Keys { get; } = Create("keys", [Parameter("value")], new ArrayTypeSymbol(TypeSymbol.String));
    public static FunctionSymbol Has { get; } = Create("has", [Parameter("value"), Parameter("name")], TypeSymbol.Bool);
    public static FunctionSymbol Clock { get; } = Create("clock", [], TypeSymbol.Double);
    public static FunctionSymbol Read { get; } = Create("read", [Parameter("path")], TypeSymbol.String);
    public static FunctionSymbol Write { get; } = Create("write", [Parameter("path"), Parameter("text")], TypeSymbol.Void);

    public static IReadOnlyList<FunctionSymbol> All { get; } =
    [
        Print,
        PrintLn,
        Type,
        Str,
        Int,
        Double,
        Bool,
        Input,
        Keys,
        Has,
        Clock,
        Read,
        Write
    ];

    private static FunctionSymbol Create(string name, IReadOnlyList<VariableSymbol> parameters, TypeSymbol returnType)
    {
        return new FunctionSymbol(name, parameters, returnType, isBuiltIn: true);
    }

    private static VariableSymbol Parameter(string name)
    {
        return new(name, TypeSymbol.Any, isParameter: true);
    }
}
