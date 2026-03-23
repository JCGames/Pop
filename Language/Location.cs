namespace Pop.Language;

public readonly struct Location
{
    public SourceFile SourceFile { get; }
    public TextSpan Span { get; }
    public int Line { get; }
    public int Column { get; }

    public Location(SourceFile sourceFile, TextSpan span, int line, int column)
    {
        SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
        Span = span;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"{SourceFile.Info?.FullName}({Line},{Column})";
}