namespace Pop.Language;

public readonly struct TextSpan
{
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;

    public TextSpan(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Start = start;
        Length = length;
    }

    public static TextSpan FromBounds(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        return new TextSpan(start, end - start);
    }

    public override string ToString() => $"[{Start}..{End})";
}
