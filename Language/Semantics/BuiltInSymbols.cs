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
    public static FunctionSymbol Int { get; } = Create("int", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol Double { get; } = Create("double", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol Bool { get; } = Create("bool", [Parameter("value")], TypeSymbol.Bool);
    public static FunctionSymbol IsError { get; } = Create("isError", [Parameter("value")], TypeSymbol.Bool);
    public static FunctionSymbol Error { get; } = Create("error", [Parameter("code"), Parameter("message")], TypeSymbol.Any);
    public static FunctionSymbol Input { get; } = Create("input", [], TypeSymbol.String);
    public static FunctionSymbol Keys { get; } = Create("keys", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol Has { get; } = Create("has", [Parameter("value"), Parameter("name")], TypeSymbol.Any);
    public static FunctionSymbol Clock { get; } = Create("clock", [], TypeSymbol.Double);

    public static FunctionSymbol MathAbs { get; } = Create("abs", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathMin { get; } = Create("min", [Parameter("left"), Parameter("right")], TypeSymbol.Any);
    public static FunctionSymbol MathMax { get; } = Create("max", [Parameter("left"), Parameter("right")], TypeSymbol.Any);
    public static FunctionSymbol MathClamp { get; } = Create("clamp", [Parameter("value"), Parameter("min"), Parameter("max")], TypeSymbol.Any);
    public static FunctionSymbol MathSqrt { get; } = Create("sqrt", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathPow { get; } = Create("pow", [Parameter("value"), Parameter("power")], TypeSymbol.Any);
    public static FunctionSymbol MathSin { get; } = Create("sin", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathCos { get; } = Create("cos", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathTan { get; } = Create("tan", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathAsin { get; } = Create("asin", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathAcos { get; } = Create("acos", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathAtan { get; } = Create("atan", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathAtan2 { get; } = Create("atan2", [Parameter("y"), Parameter("x")], TypeSymbol.Any);
    public static FunctionSymbol MathLog { get; } = Create("log", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathLog10 { get; } = Create("log10", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathLog2 { get; } = Create("log2", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathExp { get; } = Create("exp", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathFloor { get; } = Create("floor", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathCeil { get; } = Create("ceil", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathRound { get; } = Create("round", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol MathTrunc { get; } = Create("trunc", [Parameter("value")], TypeSymbol.Any);

    public static FunctionSymbol JsonParse { get; } = Create("parse", [Parameter("text")], TypeSymbol.Any);
    public static FunctionSymbol JsonStringify { get; } = Create("stringify", [Parameter("value")], TypeSymbol.Any);
    public static FunctionSymbol JsonPretty { get; } = Create("pretty", [Parameter("value")], TypeSymbol.Any);

    public static FunctionSymbol HttpGet { get; } = Create("get", [Parameter("url")], TypeSymbol.Any);
    public static FunctionSymbol HttpPost { get; } = Create("post", [Parameter("url"), Parameter("body")], TypeSymbol.Any);
    public static FunctionSymbol HttpPut { get; } = Create("put", [Parameter("url"), Parameter("body")], TypeSymbol.Any);
    public static FunctionSymbol HttpDelete { get; } = Create("delete", [Parameter("url")], TypeSymbol.Any);
    public static FunctionSymbol HttpRequest { get; } = Create("request", [Parameter("method"), Parameter("url"), Parameter("body"), Parameter("headers")], TypeSymbol.Any);

    public static FunctionSymbol FsRead { get; } = Create("read", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsWrite { get; } = Create("write", [Parameter("path"), Parameter("text")], TypeSymbol.Any);
    public static FunctionSymbol FsAppend { get; } = Create("append", [Parameter("path"), Parameter("text")], TypeSymbol.Any);
    public static FunctionSymbol FsCopy { get; } = Create("copy", [Parameter("source"), Parameter("destination")], TypeSymbol.Any);
    public static FunctionSymbol FsMove { get; } = Create("move", [Parameter("source"), Parameter("destination")], TypeSymbol.Any);
    public static FunctionSymbol FsRemove { get; } = Create("remove", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsExists { get; } = Create("exists", [Parameter("path")], TypeSymbol.Bool);
    public static FunctionSymbol FsIsFile { get; } = Create("isFile", [Parameter("path")], TypeSymbol.Bool);
    public static FunctionSymbol FsIsDir { get; } = Create("isDir", [Parameter("path")], TypeSymbol.Bool);
    public static FunctionSymbol FsInfo { get; } = Create("info", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsSize { get; } = Create("size", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsModified { get; } = Create("modified", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsCreated { get; } = Create("created", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsList { get; } = Create("list", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsFiles { get; } = Create("files", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsDirs { get; } = Create("dirs", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsMkdir { get; } = Create("mkdir", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsCwd { get; } = Create("cwd", [], TypeSymbol.String);
    public static FunctionSymbol FsChdir { get; } = Create("chdir", [Parameter("path")], TypeSymbol.Any);
    public static FunctionSymbol FsJoin { get; } = Create("join", [Parameter("left"), Parameter("right")], TypeSymbol.String);
    public static FunctionSymbol FsName { get; } = Create("name", [Parameter("path")], TypeSymbol.String);
    public static FunctionSymbol FsStem { get; } = Create("stem", [Parameter("path")], TypeSymbol.String);
    public static FunctionSymbol FsExt { get; } = Create("ext", [Parameter("path")], TypeSymbol.String);
    public static FunctionSymbol FsParent { get; } = Create("parent", [Parameter("path")], TypeSymbol.String);
    public static FunctionSymbol FsAbsolute { get; } = Create("absolute", [Parameter("path")], TypeSymbol.String);

    public static IReadOnlyList<FunctionSymbol> RootFunctions { get; } =
    [
        Print,
        PrintLn,
        Type,
        Str,
        Int,
        Double,
        Bool,
        IsError,
        Error,
        Input,
        Keys,
        Has,
        Clock
    ];

    public static IReadOnlyList<FunctionSymbol> FsFunctions { get; } =
    [
        FsRead,
        FsWrite,
        FsAppend,
        FsCopy,
        FsMove,
        FsRemove,
        FsExists,
        FsIsFile,
        FsIsDir,
        FsInfo,
        FsSize,
        FsModified,
        FsCreated,
        FsList,
        FsFiles,
        FsDirs,
        FsMkdir,
        FsCwd,
        FsChdir,
        FsJoin,
        FsName,
        FsStem,
        FsExt,
        FsParent,
        FsAbsolute
    ];

    public static IReadOnlyList<FunctionSymbol> MathFunctions { get; } =
    [
        MathAbs,
        MathMin,
        MathMax,
        MathClamp,
        MathSqrt,
        MathPow,
        MathSin,
        MathCos,
        MathTan,
        MathAsin,
        MathAcos,
        MathAtan,
        MathAtan2,
        MathLog,
        MathLog10,
        MathLog2,
        MathExp,
        MathFloor,
        MathCeil,
        MathRound,
        MathTrunc
    ];

    public static IReadOnlyList<FunctionSymbol> JsonFunctions { get; } =
    [
        JsonParse,
        JsonStringify,
        JsonPretty
    ];

    public static IReadOnlyList<FunctionSymbol> HttpFunctions { get; } =
    [
        HttpGet,
        HttpPost,
        HttpPut,
        HttpDelete,
        HttpRequest
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
