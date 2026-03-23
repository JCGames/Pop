using System.Globalization;

namespace Pop.Language;

internal sealed class Lexer
{
    private readonly SourceFile _sourceFile;
    private readonly List<Diagnostic> _diagnostics = [];
    private int _position;

    public Lexer(SourceFile sourceFile)
    {
        _sourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public List<SyntaxToken> Lex()
    {
        var tokens = new List<SyntaxToken>();

        while (true)
        {
            SkipTrivia();

            var token = LexToken();
            if (token.Kind != SyntaxKind.BadToken)
            {
                tokens.Add(token);
            }

            if (token.Kind == SyntaxKind.EndOfFileToken)
            {
                break;
            }
        }

        return tokens;
    }

    private SyntaxToken LexToken()
    {
        var start = _position;

        if (_position >= _sourceFile.Length)
        {
            return CreateToken(SyntaxKind.EndOfFileToken, start, string.Empty);
        }

        var current = _sourceFile[_position];

        if (char.IsLetter(current) || current == '_')
        {
            return LexIdentifierOrKeyword();
        }

        if (char.IsDigit(current))
        {
            return LexNumber();
        }

        return current switch
        {
            '\'' => LexCharacterLiteral(),
            '"' => LexStringLiteral(),
            '(' => AdvanceSingle(SyntaxKind.OpenParenToken),
            ')' => AdvanceSingle(SyntaxKind.CloseParenToken),
            '[' => AdvanceSingle(SyntaxKind.OpenBracketToken),
            ']' => AdvanceSingle(SyntaxKind.CloseBracketToken),
            '{' => AdvanceSingle(SyntaxKind.OpenBraceToken),
            '}' => AdvanceSingle(SyntaxKind.CloseBraceToken),
            ',' => AdvanceSingle(SyntaxKind.CommaToken),
            '.' => AdvanceSingle(SyntaxKind.DotToken),
            '@' => AdvanceSingle(SyntaxKind.AtToken),
            '?' => AdvanceSingle(SyntaxKind.QuestionToken),
            ':' => AdvanceSingle(SyntaxKind.ColonToken),
            '+' => AdvanceSingle(SyntaxKind.PlusToken),
            '-' => MatchDouble('>', SyntaxKind.ArrowToken, SyntaxKind.MinusToken),
            '*' => AdvanceSingle(SyntaxKind.StarToken),
            '/' => AdvanceSingle(SyntaxKind.SlashToken),
            '%' => AdvanceSingle(SyntaxKind.PercentToken),
            '~' => AdvanceSingle(SyntaxKind.TildeToken),
            '^' => AdvanceSingle(SyntaxKind.CaretToken),
            '!' => MatchDouble('=', SyntaxKind.BangEqualsToken, SyntaxKind.BangToken),
            '=' => MatchDouble('=', SyntaxKind.EqualsEqualsToken, SyntaxKind.BadToken),
            '<' => LexLessFamily(),
            '>' => LexGreaterFamily(),
            '&' => MatchDouble('&', SyntaxKind.AmpersandAmpersandToken, SyntaxKind.AmpersandToken),
            '|' => MatchDouble('|', SyntaxKind.PipePipeToken, SyntaxKind.PipeToken),
            _ => LexBadToken()
        };
    }

    private SyntaxToken LexIdentifierOrKeyword()
    {
        var start = _position;

        while (char.IsLetterOrDigit(_sourceFile[_position]) || _sourceFile[_position] == '_')
        {
            _position++;
        }

        var text = _sourceFile.Slice(start, _position - start);
        return text switch
        {
            "var" => new SyntaxToken(SyntaxKind.VarKeyword, new TextSpan(start, text.Length), text),
            "public" => new SyntaxToken(SyntaxKind.PublicKeyword, new TextSpan(start, text.Length), text),
            "fun" => new SyntaxToken(SyntaxKind.FunKeyword, new TextSpan(start, text.Length), text),
            "ret" => new SyntaxToken(SyntaxKind.RetKeyword, new TextSpan(start, text.Length), text),
            "cont" => new SyntaxToken(SyntaxKind.ContKeyword, new TextSpan(start, text.Length), text),
            "abort" => new SyntaxToken(SyntaxKind.AbortKeyword, new TextSpan(start, text.Length), text),
            "inject" => new SyntaxToken(SyntaxKind.InjectKeyword, new TextSpan(start, text.Length), text),
            "while" => new SyntaxToken(SyntaxKind.WhileKeyword, new TextSpan(start, text.Length), text),
            "if" => new SyntaxToken(SyntaxKind.IfKeyword, new TextSpan(start, text.Length), text),
            "else" => new SyntaxToken(SyntaxKind.ElseKeyword, new TextSpan(start, text.Length), text),
            "true" => new SyntaxToken(SyntaxKind.TrueKeyword, new TextSpan(start, text.Length), text, true),
            "false" => new SyntaxToken(SyntaxKind.FalseKeyword, new TextSpan(start, text.Length), text, false),
            "nil" => new SyntaxToken(SyntaxKind.NilKeyword, new TextSpan(start, text.Length), text, null),
            _ => new SyntaxToken(SyntaxKind.IdentifierToken, new TextSpan(start, text.Length), text)
        };
    }

    private SyntaxToken LexNumber()
    {
        var start = _position;
        var hasDecimalPoint = false;

        while (char.IsDigit(_sourceFile[_position]) || (!hasDecimalPoint && _sourceFile[_position] == '.'))
        {
            if (_sourceFile[_position] == '.')
            {
                hasDecimalPoint = true;
            }

            _position++;
        }

        var text = _sourceFile.Slice(start, _position - start);
        if (hasDecimalPoint)
        {
            if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var floatValue))
            {
                ReportError(new TextSpan(start, text.Length), $"Invalid floating-point literal '{text}'.");
                floatValue = 0;
            }

            return new SyntaxToken(SyntaxKind.FloatLiteralToken, new TextSpan(start, text.Length), text, floatValue);
        }

        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var integerValue))
        {
            ReportError(new TextSpan(start, text.Length), $"Invalid integer literal '{text}'.");
            integerValue = 0;
        }

        return new SyntaxToken(SyntaxKind.IntegerLiteralToken, new TextSpan(start, text.Length), text, integerValue);
    }

    private SyntaxToken LexCharacterLiteral()
    {
        var start = _position++;
        char value;

        if (_position >= _sourceFile.Length || _sourceFile[_position] is '\r' or '\n' or '\'')
        {
            ReportError(new TextSpan(start, Math.Min(1, _sourceFile.Length - start)), "Invalid character literal.");
            return CreateToken(SyntaxKind.CharacterLiteralToken, start, _sourceFile.Slice(start, Math.Min(_position - start, _sourceFile.Length - start)), '\0');
        }

        value = _sourceFile[_position] == '\\'
            ? ReadEscapedCharacter()
            : _sourceFile.Read(ref _position);

        if (_sourceFile[_position] != '\'')
        {
            ReportError(TextSpan.FromBounds(start, _position), "Unterminated character literal.");
            return CreateToken(SyntaxKind.CharacterLiteralToken, start, _sourceFile.Slice(start, _position - start), value);
        }

        _position++;
        return new SyntaxToken(
            SyntaxKind.CharacterLiteralToken,
            TextSpan.FromBounds(start, _position),
            _sourceFile.Slice(start, _position - start),
            value);
    }

    private SyntaxToken LexStringLiteral()
    {
        var start = _position++;
        var value = new System.Text.StringBuilder();

        while (_position < _sourceFile.Length && _sourceFile[_position] != '"' && _sourceFile[_position] is not '\r' and not '\n')
        {
            value.Append(_sourceFile[_position] == '\\' ? ReadEscapedCharacter() : _sourceFile.Read(ref _position));
        }

        if (_sourceFile[_position] != '"')
        {
            ReportError(TextSpan.FromBounds(start, _position), "Unterminated string literal.");
            return new SyntaxToken(
                SyntaxKind.StringLiteralToken,
                TextSpan.FromBounds(start, _position),
                _sourceFile.Slice(start, _position - start),
                value.ToString());
        }

        _position++;
        return new SyntaxToken(
            SyntaxKind.StringLiteralToken,
            TextSpan.FromBounds(start, _position),
            _sourceFile.Slice(start, _position - start),
            value.ToString());
    }

    private char ReadEscapedCharacter()
    {
        var escapeStart = _position++;
        if (_position >= _sourceFile.Length)
        {
            ReportError(new TextSpan(escapeStart, 1), "Incomplete escape sequence.");
            return '\0';
        }

        var escape = _sourceFile.Read(ref _position);
        return escape switch
        {
            '\'' => '\'',
            '"' => '"',
            '\\' => '\\',
            '0' => '\0',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            _ => ReportUnknownEscape(escapeStart, escape)
        };
    }

    private char ReportUnknownEscape(int start, char escape)
    {
        ReportError(new TextSpan(start, 2), $"Unknown escape sequence '\\{escape}'.");
        return escape;
    }

    private SyntaxToken LexLessFamily()
    {
        if (Peek(1) == '<')
        {
            return AdvanceDouble(SyntaxKind.LessLessToken);
        }

        return MatchDouble('=', SyntaxKind.LessEqualsToken, SyntaxKind.LessToken);
    }

    private SyntaxToken LexGreaterFamily()
    {
        if (Peek(1) == '>')
        {
            return AdvanceDouble(SyntaxKind.GreaterGreaterToken);
        }

        return MatchDouble('=', SyntaxKind.GreaterEqualsToken, SyntaxKind.GreaterToken);
    }

    private SyntaxToken LexBadToken()
    {
        var start = _position;
        var text = _sourceFile.Slice(_position, 1);
        _position++;
        ReportError(new TextSpan(start, 1), $"Unexpected character '{text}'.");
        return new SyntaxToken(SyntaxKind.BadToken, new TextSpan(start, 1), text);
    }

    private SyntaxToken AdvanceSingle(SyntaxKind kind)
    {
        var start = _position++;
        return CreateToken(kind, start, _sourceFile.Slice(start, 1));
    }

    private SyntaxToken AdvanceDouble(SyntaxKind kind)
    {
        var start = _position;
        _position += 2;
        return CreateToken(kind, start, _sourceFile.Slice(start, 2));
    }

    private SyntaxToken MatchDouble(char second, SyntaxKind pairKind, SyntaxKind singleKind)
    {
        if (Peek(1) == second)
        {
            return AdvanceDouble(pairKind);
        }

        if (singleKind == SyntaxKind.BadToken)
        {
            return LexBadToken();
        }

        return AdvanceSingle(singleKind);
    }

    private SyntaxToken CreateToken(SyntaxKind kind, int start, string text, object? value = null)
    {
        return new SyntaxToken(kind, new TextSpan(start, text.Length), text, value);
    }

    private char Peek(int offset)
    {
        return _sourceFile.Peek(_position + offset);
    }

    private void SkipTrivia()
    {
        while (true)
        {
            while (char.IsWhiteSpace(_sourceFile[_position]))
            {
                _position++;
            }

            if (_sourceFile[_position] == '/' && Peek(1) == '/')
            {
                _position += 2;
                while (_sourceFile[_position] is not '\0' and not '\r' and not '\n')
                {
                    _position++;
                }

                continue;
            }

            break;
        }
    }

    private void ReportError(TextSpan span, string message)
    {
        _diagnostics.Add(new Diagnostic(message, _sourceFile.GetLocation(span), DiagnosticLevel.Error));
    }
}
