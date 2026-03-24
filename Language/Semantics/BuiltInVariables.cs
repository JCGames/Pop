namespace Pop.Language;

public static class BuiltInVariables
{
    private static readonly ObjectTypeSymbol CornFsType = new(
        BuiltInSymbols.FsFunctions.ToDictionary(
            static function => function.Name,
            static function => (TypeSymbol)function.Type,
            StringComparer.Ordinal));

    private static readonly ObjectTypeSymbol CornMathType = new(
        BuildMathProperties());

    private static readonly ObjectTypeSymbol CornJsonType = new(
        BuiltInSymbols.JsonFunctions.ToDictionary(
            static function => function.Name,
            static function => (TypeSymbol)function.Type,
            StringComparer.Ordinal));

    private static readonly ObjectTypeSymbol CornHttpType = new(
        BuiltInSymbols.HttpFunctions.ToDictionary(
            static function => function.Name,
            static function => (TypeSymbol)function.Type,
            StringComparer.Ordinal));

    private static readonly ObjectTypeSymbol CornType = new(
        BuildCornProperties());

    public static VariableSymbol Corn { get; } = new("corn", CornType);

    public static IReadOnlyList<VariableSymbol> All { get; } =
    [
        Corn
    ];

    private static Dictionary<string, TypeSymbol> BuildCornProperties()
    {
        var properties = BuiltInSymbols.RootFunctions.ToDictionary(
            static function => function.Name,
            static function => (TypeSymbol)function.Type,
            StringComparer.Ordinal);
        properties["fs"] = CornFsType;
        properties["math"] = CornMathType;
        properties["json"] = CornJsonType;
        properties["http"] = CornHttpType;
        return properties;
    }

    private static Dictionary<string, TypeSymbol> BuildMathProperties()
    {
        var properties = BuiltInSymbols.MathFunctions.ToDictionary(
            static function => function.Name,
            static function => (TypeSymbol)function.Type,
            StringComparer.Ordinal);
        properties["pi"] = TypeSymbol.Double;
        properties["tau"] = TypeSymbol.Double;
        properties["e"] = TypeSymbol.Double;
        return properties;
    }
}
