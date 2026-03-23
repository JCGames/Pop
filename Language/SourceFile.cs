using System;
using System.Collections.Generic;
using System.IO;

namespace Pop.Language;

public sealed class SourceFile
{
    private readonly int[] _lineStarts;

    public FileInfo? Info { get; }
    public string Text { get; }
    public int Length => Text.Length;

    private SourceFile(string text, FileInfo? info)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        Info = info;
        _lineStarts = ComputeLineStarts(text);
    }

    public static SourceFile Load(FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        return new SourceFile(File.ReadAllText(fileInfo.FullName), fileInfo);
    }

    public static SourceFile FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new SourceFile(text, null);
    }

    public char this[int position] => Peek(position);

    public bool IsEnd(int position) => position >= Text.Length;

    public char Peek(int position)
    {
        if (position < 0 || position >= Text.Length)
        {
            return '\0';
        }

        return Text[position];
    }

    public char Read(ref int position)
    {
        if (position < 0 || position >= Text.Length)
        {
            return '\0';
        }

        return Text[position++];
    }

    public string Slice(int start, int length)
    {
        ValidateSpan(start, length);
        return Text.Substring(start, length);
    }

    public string Slice(TextSpan span) => Slice(span.Start, span.Length);

    public Location GetLocation(int position)
    {
        if (position < 0 || position > Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        var lineIndex = GetLineIndex(position);
        var lineStart = _lineStarts[lineIndex];
        var column = position - lineStart;

        return new Location(
            this,
            new TextSpan(position, 0),
            lineIndex + 1,
            column + 1);
    }

    public Location GetLocation(TextSpan span)
    {
        ValidateSpan(span.Start, span.Length);

        var lineIndex = GetLineIndex(span.Start);
        var lineStart = _lineStarts[lineIndex];
        var column = span.Start - lineStart;

        return new Location(
            this,
            span,
            lineIndex + 1,
            column + 1);
    }

    public string GetLineText(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _lineStarts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        }

        var index = lineNumber - 1;
        var start = _lineStarts[index];
        var end = index + 1 < _lineStarts.Length ? _lineStarts[index + 1] : Text.Length;

        while (end > start && (Text[end - 1] == '\r' || Text[end - 1] == '\n'))
        {
            end--;
        }

        return Text.Substring(start, end - start);
    }

    public string FormatDiagnostic(string message, TextSpan span)
    {
        var location = GetLocation(span);
        var lineText = GetLineText(location.Line);
        
        var caretLine = 
            new string(' ', Math.Max(0, location.Column - 1)) +
            new string('^', Math.Max(1, span.Length));

        return $"{Info?.FullName}({location.Line},{location.Column}): {message}{Environment.NewLine}" +
               $"{lineText}{Environment.NewLine}" +
               $"{caretLine}";
    }

    private int GetLineIndex(int position)
    {
        var index = Array.BinarySearch(_lineStarts, position);

        if (index >= 0)
        {
            return index;
        }

        return ~index - 1;
    }

    private static int[] ComputeLineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            switch (c)
            {
                case '\r':
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    starts.Add(i + 1);

                    break;
                }
                case '\n':
                    starts.Add(i + 1);
                    break;
            }
        }

        return starts.ToArray();
    }

    private void ValidateSpan(int start, int length)
    {
        if (start < 0 || start > Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length < 0 || length > Text.Length - start)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
    }
}

