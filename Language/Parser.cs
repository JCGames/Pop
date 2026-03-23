namespace Pop.Language;

public sealed class Parser
{
    private readonly SourceFile _sourceFile;
    private readonly IReadOnlyList<SyntaxToken> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _position;

    private Parser(SourceFile sourceFile)
    {
        _sourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));

        var lexer = new Lexer(sourceFile);
        _tokens = lexer.Lex();
        _diagnostics = [.. lexer.Diagnostics];
    }

    public static ParseResult Parse(SourceFile sourceFile)
    {
        var parser = new Parser(sourceFile);
        var root = parser.ParseCompilationUnit();
        return new ParseResult(sourceFile, root, parser._diagnostics);
    }

    public static ParseResult ParseText(string text)
    {
        return Parse(SourceFile.FromText(text));
    }

    private CompilationUnitSyntax ParseCompilationUnit()
    {
        var statements = new List<StatementSyntax>();

        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            var startToken = Current;
            statements.Add(ParseStatement());

            if (Current == startToken)
            {
                NextToken();
            }
        }

        var endOfFileToken = Match(SyntaxKind.EndOfFileToken);
        return new CompilationUnitSyntax(statements, endOfFileToken);
    }

    private StatementSyntax ParseStatement()
    {
        return Current.Kind switch
        {
            SyntaxKind.OpenBraceToken => ParseBlockStatement(),
            SyntaxKind.PublicKeyword when Peek(1).Kind == SyntaxKind.FunKeyword => ParseFunctionDeclarationStatement(),
            SyntaxKind.FunKeyword => ParseFunctionDeclarationStatement(),
            SyntaxKind.RetKeyword => ParseReturnStatement(),
            SyntaxKind.ContKeyword => ParseContinueStatement(),
            SyntaxKind.AbortKeyword => ParseBreakStatement(),
            SyntaxKind.WhileKeyword => ParseWhileStatement(),
            SyntaxKind.IfKeyword => ParseIfStatement(),
            _ => new ExpressionStatementSyntax(ParseExpression())
        };
    }

    private ContinueStatementSyntax ParseContinueStatement()
    {
        var contKeyword = Match(SyntaxKind.ContKeyword);
        return new ContinueStatementSyntax(contKeyword);
    }

    private BreakStatementSyntax ParseBreakStatement()
    {
        var abortKeyword = Match(SyntaxKind.AbortKeyword);
        return new BreakStatementSyntax(abortKeyword);
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
        var retKeyword = Match(SyntaxKind.RetKeyword);

        // A bare `ret` is allowed; otherwise parse the returned expression.
        var expression = Current.Kind is SyntaxKind.CloseBraceToken or SyntaxKind.EndOfFileToken
            ? null
            : ParseExpression();

        return new ReturnStatementSyntax(retKeyword, expression);
    }

    private FunctionDeclarationStatementSyntax ParseFunctionDeclarationStatement()
    {
        var publicKeyword = Current.Kind == SyntaxKind.PublicKeyword ? Match(SyntaxKind.PublicKeyword) : null;
        var funKeyword = Match(SyntaxKind.FunKeyword);
        var identifierToken = Match(SyntaxKind.IdentifierToken);
        var openParenToken = Match(SyntaxKind.OpenParenToken);
        var parameters = new List<ParameterSyntax>();

        while (Current.Kind is not SyntaxKind.CloseParenToken and not SyntaxKind.EndOfFileToken)
        {
            parameters.Add(new ParameterSyntax(Match(SyntaxKind.IdentifierToken)));

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            NextToken();
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken);
        var body = ParseBlockStatement();
        return new FunctionDeclarationStatementSyntax(
            publicKeyword,
            funKeyword,
            identifierToken,
            openParenToken,
            parameters,
            closeParenToken,
            body);
    }

    private BlockStatementSyntax ParseBlockStatement()
    {
        var openBraceToken = Match(SyntaxKind.OpenBraceToken);
        var statements = new List<StatementSyntax>();

        while (Current.Kind is not SyntaxKind.CloseBraceToken and not SyntaxKind.EndOfFileToken)
        {
            var startToken = Current;
            statements.Add(ParseStatement());

            if (Current == startToken)
            {
                NextToken();
            }
        }

        var closeBraceToken = Match(SyntaxKind.CloseBraceToken);
        return new BlockStatementSyntax(openBraceToken, statements, closeBraceToken);
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        var whileKeyword = Match(SyntaxKind.WhileKeyword);
        var condition = ParseExpression();
        var body = ParseBlockStatement();
        return new WhileStatementSyntax(whileKeyword, condition, body);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = Match(SyntaxKind.IfKeyword);
        var condition = ParseExpression();
        var thenStatement = ParseBlockStatement();
        var elseClause = ParseElseClause();
        return new IfStatementSyntax(ifKeyword, condition, thenStatement, elseClause);
    }

    private ElseClauseSyntax? ParseElseClause()
    {
        if (Current.Kind != SyntaxKind.ElseKeyword)
        {
            return null;
        }

        var elseKeyword = Match(SyntaxKind.ElseKeyword);
        StatementSyntax statement = Current.Kind == SyntaxKind.IfKeyword
            ? ParseIfStatement()
            : ParseBlockStatement();

        return new ElseClauseSyntax(elseKeyword, statement);
    }

    private ExpressionSyntax ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    private ExpressionSyntax ParseAssignmentExpression()
    {
        if (Current.Kind == SyntaxKind.PublicKeyword && Peek(1).Kind == SyntaxKind.VarKeyword)
        {
            var publicKeyword = Match(SyntaxKind.PublicKeyword);
            var varKeyword = Match(SyntaxKind.VarKeyword);
            var identifierToken = Match(SyntaxKind.IdentifierToken);
            var arrowToken = Match(SyntaxKind.ArrowToken);
            var initializer = ParseAssignmentExpression();
            return new VariableDeclarationExpressionSyntax(publicKeyword, varKeyword, identifierToken, arrowToken, initializer);
        }

        if (Current.Kind == SyntaxKind.VarKeyword)
        {
            var varKeyword = NextToken();
            var identifierToken = Match(SyntaxKind.IdentifierToken);
            var arrowToken = Match(SyntaxKind.ArrowToken);
            var initializer = ParseAssignmentExpression();
            return new VariableDeclarationExpressionSyntax(null, varKeyword, identifierToken, arrowToken, initializer);
        }

        if (Current.Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.ArrowToken)
        {
            var identifierToken = NextToken();
            var arrowToken = NextToken();
            var expression = ParseAssignmentExpression();
            return new AssignmentExpressionSyntax(identifierToken, arrowToken, expression);
        }

        return ParseConditionalExpression();
    }

    private ExpressionSyntax ParseConditionalExpression()
    {
        var condition = ParseBinaryExpression(0);

        if (Current.Kind != SyntaxKind.QuestionToken)
        {
            return condition;
        }

        var questionToken = NextToken();
        var whenTrue = ParseExpression();
        var colonToken = Match(SyntaxKind.ColonToken);
        var whenFalse = ParseExpression();
        return new ConditionalExpressionSyntax(condition, questionToken, whenTrue, colonToken, whenFalse);
    }

    private ExpressionSyntax ParseBinaryExpression(int parentPrecedence)
    {
        ExpressionSyntax left;
        var unaryPrecedence = GetUnaryPrecedence(Current.Kind);
        if (unaryPrecedence > parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseBinaryExpression(unaryPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePostfixExpression();
        }

        while (true)
        {
            var precedence = GetBinaryPrecedence(Current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePostfixExpression()
    {
        var expression = ParsePrimaryExpression();

        while (Current.Kind is SyntaxKind.OpenParenToken or SyntaxKind.DotToken)
        {
            expression = Current.Kind switch
            {
                SyntaxKind.OpenParenToken => ParseCallExpression(expression),
                SyntaxKind.DotToken => ParseMemberAccessExpression(expression),
                _ => expression
            };
        }

        return expression;
    }

    private MemberAccessExpressionSyntax ParseMemberAccessExpression(ExpressionSyntax target)
    {
        var dotToken = Match(SyntaxKind.DotToken);
        var identifierToken = Match(SyntaxKind.IdentifierToken);
        return new MemberAccessExpressionSyntax(target, dotToken, identifierToken);
    }

    private CallExpressionSyntax ParseCallExpression(ExpressionSyntax target)
    {
        var openParenToken = Match(SyntaxKind.OpenParenToken);
        var arguments = new List<ExpressionSyntax>();

        while (Current.Kind is not SyntaxKind.CloseParenToken and not SyntaxKind.EndOfFileToken)
        {
            arguments.Add(ParseExpression());

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            NextToken();
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken);
        return new CallExpressionSyntax(target, openParenToken, arguments, closeParenToken);
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        switch (Current.Kind)
        {
            case SyntaxKind.AtToken:
                return ParseLambdaExpression();
            case SyntaxKind.InjectKeyword:
                return ParseInjectExpression();
            case SyntaxKind.OpenBracketToken:
                return ParseArrayExpression();
            case SyntaxKind.OpenBraceToken:
                return ParseObjectExpression();
            case SyntaxKind.OpenParenToken:
            {
                var openParen = NextToken();
                var expression = ParseExpression();
                var closeParen = Match(SyntaxKind.CloseParenToken);
                return new ParenthesizedExpressionSyntax(openParen, expression, closeParen);
            }
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            case SyntaxKind.IntegerLiteralToken:
            case SyntaxKind.FloatLiteralToken:
            case SyntaxKind.CharacterLiteralToken:
            case SyntaxKind.StringLiteralToken:
                return new LiteralExpressionSyntax(NextToken());
            case SyntaxKind.IdentifierToken:
                return new NameExpressionSyntax(NextToken());
            default:
            {
                var missingLiteral = Match(SyntaxKind.IntegerLiteralToken);
                return new LiteralExpressionSyntax(missingLiteral);
            }
        }
    }

    private InjectExpressionSyntax ParseInjectExpression()
    {
        var injectKeyword = Match(SyntaxKind.InjectKeyword);
        var pathToken = Match(SyntaxKind.StringLiteralToken);
        return new InjectExpressionSyntax(injectKeyword, pathToken);
    }

    private LambdaExpressionSyntax ParseLambdaExpression()
    {
        var atToken = Match(SyntaxKind.AtToken);
        var openParenToken = Match(SyntaxKind.OpenParenToken);
        var parameters = new List<ParameterSyntax>();

        while (Current.Kind is not SyntaxKind.CloseParenToken and not SyntaxKind.EndOfFileToken)
        {
            parameters.Add(new ParameterSyntax(Match(SyntaxKind.IdentifierToken)));

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            NextToken();
        }

        var closeParenToken = Match(SyntaxKind.CloseParenToken);
        var body = ParseBlockStatement();
        return new LambdaExpressionSyntax(atToken, openParenToken, parameters, closeParenToken, body);
    }

    private ArrayExpressionSyntax ParseArrayExpression()
    {
        var openBracketToken = Match(SyntaxKind.OpenBracketToken);
        var elements = new List<ExpressionSyntax>();

        while (Current.Kind is not SyntaxKind.CloseBracketToken and not SyntaxKind.EndOfFileToken)
        {
            elements.Add(ParseExpression());

            if (Current.Kind != SyntaxKind.CommaToken)
            {
                break;
            }

            NextToken();
        }

        var closeBracketToken = Match(SyntaxKind.CloseBracketToken);
        return new ArrayExpressionSyntax(openBracketToken, elements, closeBracketToken);
    }

    private ObjectExpressionSyntax ParseObjectExpression()
    {
        var openBraceToken = Match(SyntaxKind.OpenBraceToken);
        var properties = new List<ObjectPropertySyntax>();

        while (Current.Kind is not SyntaxKind.CloseBraceToken and not SyntaxKind.EndOfFileToken)
        {
            var identifierToken = Match(SyntaxKind.IdentifierToken);
            var colonToken = Match(SyntaxKind.ColonToken);
            var value = ParseExpression();
            properties.Add(new ObjectPropertySyntax(identifierToken, colonToken, value));
        }

        var closeBraceToken = Match(SyntaxKind.CloseBraceToken);
        return new ObjectExpressionSyntax(openBraceToken, properties, closeBraceToken);
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }

        _diagnostics.Add(new Diagnostic(
            $"Expected {GetDisplayName(kind)} but found {GetDisplayName(Current.Kind)}.",
            _sourceFile.GetLocation(new TextSpan(Current.Span.Start, 0)),
            DiagnosticLevel.Error));

        return new SyntaxToken(kind, new TextSpan(Current.Span.Start, 0), string.Empty, GetDefaultValue(kind));
    }

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[index];
    }

    private SyntaxToken Current => Peek(0);

    private static int GetUnaryPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.PlusToken => 11,
            SyntaxKind.MinusToken => 11,
            SyntaxKind.BangToken => 11,
            SyntaxKind.TildeToken => 11,
            _ => 0
        };
    }

    private static int GetBinaryPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.StarToken => 10,
            SyntaxKind.SlashToken => 10,
            SyntaxKind.PercentToken => 10,
            SyntaxKind.PlusToken => 9,
            SyntaxKind.MinusToken => 9,
            SyntaxKind.LessLessToken => 8,
            SyntaxKind.GreaterGreaterToken => 8,
            SyntaxKind.LessToken => 7,
            SyntaxKind.LessEqualsToken => 7,
            SyntaxKind.GreaterToken => 7,
            SyntaxKind.GreaterEqualsToken => 7,
            SyntaxKind.EqualsEqualsToken => 6,
            SyntaxKind.BangEqualsToken => 6,
            SyntaxKind.AmpersandToken => 5,
            SyntaxKind.CaretToken => 4,
            SyntaxKind.PipeToken => 3,
            SyntaxKind.AmpersandAmpersandToken => 2,
            SyntaxKind.PipePipeToken => 1,
            _ => 0
        };
    }

    private static string GetDisplayName(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.EndOfFileToken => "end of file",
            SyntaxKind.IdentifierToken => "identifier",
            SyntaxKind.IntegerLiteralToken => "integer literal",
            SyntaxKind.FloatLiteralToken => "floating-point literal",
            SyntaxKind.CharacterLiteralToken => "character literal",
            SyntaxKind.StringLiteralToken => "string literal",
            SyntaxKind.VarKeyword => "'var'",
            SyntaxKind.PublicKeyword => "'public'",
            SyntaxKind.FunKeyword => "'fun'",
            SyntaxKind.RetKeyword => "'ret'",
            SyntaxKind.ContKeyword => "'cont'",
            SyntaxKind.AbortKeyword => "'abort'",
            SyntaxKind.InjectKeyword => "'inject'",
            SyntaxKind.WhileKeyword => "'while'",
            SyntaxKind.IfKeyword => "'if'",
            SyntaxKind.ElseKeyword => "'else'",
            SyntaxKind.OpenParenToken => "'('",
            SyntaxKind.CloseParenToken => "')'",
            SyntaxKind.OpenBracketToken => "'['",
            SyntaxKind.CloseBracketToken => "']'",
            SyntaxKind.OpenBraceToken => "'{'",
            SyntaxKind.CloseBraceToken => "'}'",
            SyntaxKind.CommaToken => "','",
            SyntaxKind.DotToken => "'.'",
            SyntaxKind.AtToken => "'@'",
            SyntaxKind.QuestionToken => "'?'",
            SyntaxKind.ColonToken => "':'",
            SyntaxKind.PlusToken => "'+'",
            SyntaxKind.MinusToken => "'-'",
            SyntaxKind.StarToken => "'*'",
            SyntaxKind.SlashToken => "'/'",
            SyntaxKind.PercentToken => "'%'",
            SyntaxKind.BangToken => "'!'",
            SyntaxKind.TildeToken => "'~'",
            SyntaxKind.ArrowToken => "'->'",
            SyntaxKind.EqualsEqualsToken => "'=='",
            SyntaxKind.BangEqualsToken => "'!='",
            SyntaxKind.LessToken => "'<'",
            SyntaxKind.LessEqualsToken => "'<='",
            SyntaxKind.GreaterToken => "'>'",
            SyntaxKind.GreaterEqualsToken => "'>='",
            SyntaxKind.LessLessToken => "'<<'",
            SyntaxKind.GreaterGreaterToken => "'>>'",
            SyntaxKind.AmpersandToken => "'&'",
            SyntaxKind.AmpersandAmpersandToken => "'&&'",
            SyntaxKind.PipeToken => "'|'",
            SyntaxKind.PipePipeToken => "'||'",
            SyntaxKind.CaretToken => "'^'",
            SyntaxKind.TrueKeyword => "'true'",
            SyntaxKind.FalseKeyword => "'false'",
            _ => kind.ToString()
        };
    }

    private static object? GetDefaultValue(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.IntegerLiteralToken => 0L,
            SyntaxKind.FloatLiteralToken => 0d,
            SyntaxKind.CharacterLiteralToken => '\0',
            SyntaxKind.StringLiteralToken => string.Empty,
            SyntaxKind.TrueKeyword => true,
            SyntaxKind.FalseKeyword => false,
            _ => null
        };
    }
}
