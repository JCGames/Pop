namespace Pop.Language;

public enum DiagnosticLevel
{
    Information = 0,
    Warning = 1,
    Error = 2
}

public sealed class Diagnostic(string message, Location location, DiagnosticLevel level)
{
    public string Message { get; } = message;
    public Location Location { get; } = location;
    public DiagnosticLevel Level { get; } = level;

    public override string ToString()
    {
        return Location.SourceFile.FormatDiagnostic(Message, Location.Span);
    }
}