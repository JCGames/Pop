namespace Pop.Runtime;

internal static class RuntimeError
{
    private const string MarkerKey = "__error";

    public static IDictionary<string, object?> Create(string code, string message)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MarkerKey] = true,
            ["code"] = code,
            ["message"] = message
        };
    }

    public static bool IsError(object? value)
    {
        return value is IReadOnlyDictionary<string, object?> properties &&
               properties.TryGetValue(MarkerKey, out var marker) &&
               Equals(marker, true);
    }

    public static object? PropagateFirst(IReadOnlyList<object?> values)
    {
        foreach (var value in values)
        {
            if (IsError(value))
            {
                return value;
            }
        }

        return null;
    }

    public static object? Propagate(params object?[] values) => PropagateFirst(values);
}
