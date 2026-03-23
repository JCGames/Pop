namespace Pop.Language;

public static class BuiltInVariables
{
    private static readonly ObjectTypeSymbol CornType = new(
        BuiltInSymbols.All.ToDictionary(
            static function => function.Name,
            static function => (TypeSymbol)function.Type,
            StringComparer.Ordinal));

    public static VariableSymbol Corn { get; } = new("corn", CornType);

    public static IReadOnlyList<VariableSymbol> All { get; } =
    [
        Corn
    ];
}
