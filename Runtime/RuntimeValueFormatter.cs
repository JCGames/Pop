namespace Pop.Runtime;

internal static class RuntimeValueFormatter
{
    public static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            bool boolean => boolean ? "true" : "false",
            string text => text,
            char character => character.ToString(),
            IReadOnlyDictionary<string, object?> properties => "{ " + string.Join(", ", properties.Select(property => property.Key + ": " + Format(property.Value))) + " }",
            IEnumerable<object?> values => "[" + string.Join(", ", values.Select(Format)) + "]",
            _ => value.ToString() ?? string.Empty
        };
    }
}
