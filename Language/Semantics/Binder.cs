namespace Pop.Language;

internal sealed class Binder
{
    private readonly ParseResult _parseResult;
    private readonly List<Diagnostic> _diagnostics = [];
    private BoundScope _scope;
    private int _loopDepth;
    private List<TypeSymbol>? _currentReturnTypes;

    public Binder(ParseResult parseResult)
    {
        _parseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
        _scope = new BoundScope(null);
        DeclareBuiltInVariables();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public IReadOnlyList<VariableSymbol> GlobalVariables => _scope.GetDeclaredVariables();
    public IReadOnlyList<FunctionSymbol> GlobalFunctions => _scope.GetDeclaredFunctions();
    public IReadOnlyList<VariableSymbol> PublicVariables => _scope.GetDeclaredVariables().Where(static variable => variable.IsPublic).ToArray();
    public IReadOnlyList<FunctionSymbol> PublicFunctions => _scope.GetDeclaredFunctions().Where(static function => function.IsPublic).ToArray();

    public BoundCompilationUnit BindCompilationUnit()
    {
        PredeclareFunctions(_parseResult.Root.Statements);

        var statements = new List<BoundStatement>();
        foreach (var statement in _parseResult.Root.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        return new BoundCompilationUnit(statements);
    }

    private BoundStatement BindStatement(StatementSyntax statement)
    {
        return statement switch
        {
            BlockStatementSyntax block => BindBlockStatement(block),
            ExpressionStatementSyntax expressionStatement => new BoundExpressionStatement(BindExpression(expressionStatement.Expression)),
            WhileStatementSyntax whileStatement => BindWhileStatement(whileStatement),
            IfStatementSyntax ifStatement => BindIfStatement(ifStatement),
            ReturnStatementSyntax returnStatement => BindReturnStatement(returnStatement),
            ContinueStatementSyntax continueStatement => BindContinueStatement(continueStatement),
            BreakStatementSyntax breakStatement => BindBreakStatement(breakStatement),
            FunctionDeclarationStatementSyntax functionDeclaration => BindFunctionDeclaration(functionDeclaration),
            _ => throw new InvalidOperationException($"Unsupported statement syntax '{statement.Kind}'.")
        };
    }

    private BoundBlockStatement BindBlockStatement(BlockStatementSyntax blockStatement)
    {
        var previousScope = _scope;
        _scope = new BoundScope(previousScope);
        PredeclareFunctions(blockStatement.Statements);

        var statements = new List<BoundStatement>();
        foreach (var statement in blockStatement.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        _scope = previousScope;
        return new BoundBlockStatement(statements);
    }

    private BoundStatement BindWhileStatement(WhileStatementSyntax whileStatement)
    {
        var condition = BindExpression(whileStatement.Condition);
        RequireType(condition.Type, TypeSymbol.Bool, whileStatement.Condition.Span, "While conditions must be bool.");

        _loopDepth++;
        var body = BindBlockStatement(whileStatement.Body);
        _loopDepth--;

        return new BoundWhileStatement(condition, body);
    }

    private BoundStatement BindIfStatement(IfStatementSyntax ifStatement)
    {
        var condition = BindExpression(ifStatement.Condition);
        RequireType(condition.Type, TypeSymbol.Bool, ifStatement.Condition.Span, "If conditions must be bool.");

        var thenStatement = BindBlockStatement(ifStatement.ThenStatement);
        var elseStatement = ifStatement.ElseClause is null
            ? null
            : BindStatement(ifStatement.ElseClause.Statement);

        return new BoundIfStatement(condition, thenStatement, elseStatement);
    }

    private BoundStatement BindReturnStatement(ReturnStatementSyntax returnStatement)
    {
        if (_currentReturnTypes is null)
        {
            Report(returnStatement.Span, "`ret` can only be used inside a function or lambda.");
        }

        var expression = returnStatement.Expression is null
            ? null
            : BindExpression(returnStatement.Expression);

        _currentReturnTypes?.Add(expression?.Type ?? TypeSymbol.Void);
        return new BoundReturnStatement(expression);
    }

    private BoundStatement BindContinueStatement(ContinueStatementSyntax continueStatement)
    {
        if (_loopDepth == 0)
        {
            Report(continueStatement.Span, "`cont` can only be used inside a loop.");
        }

        return new BoundContinueStatement();
    }

    private BoundStatement BindBreakStatement(BreakStatementSyntax breakStatement)
    {
        if (_loopDepth == 0)
        {
            Report(breakStatement.Span, "`abort` can only be used inside a loop.");
        }

        return new BoundBreakStatement();
    }

    private BoundStatement BindFunctionDeclaration(FunctionDeclarationStatementSyntax functionDeclaration)
    {
        if (!_scope.TryLookupFunction(functionDeclaration.IdentifierToken.Text, out var functionSymbol))
        {
            throw new InvalidOperationException($"Function '{functionDeclaration.IdentifierToken.Text}' was not predeclared.");
        }

        var body = BindCallableBody(functionSymbol.Parameters, functionDeclaration.Body, out var returnType);
        functionSymbol.SetReturnType(returnType);
        return new BoundFunctionDeclarationStatement(functionSymbol, body);
    }

    private BoundExpression BindExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal => BindLiteralExpression(literal),
            InjectExpressionSyntax injectExpression => BindInjectExpression(injectExpression),
            NameExpressionSyntax name => BindNameExpression(name),
            ParenthesizedExpressionSyntax parenthesized => BindExpression(parenthesized.Expression),
            VariableDeclarationExpressionSyntax declaration => BindVariableDeclarationExpression(declaration),
            AssignmentExpressionSyntax assignment => BindAssignmentExpression(assignment),
            UnaryExpressionSyntax unary => BindUnaryExpression(unary),
            BinaryExpressionSyntax binary => BindBinaryExpression(binary),
            ConditionalExpressionSyntax conditional => BindConditionalExpression(conditional),
            CallExpressionSyntax call => BindCallExpression(call),
            MemberAccessExpressionSyntax memberAccess => BindMemberAccessExpression(memberAccess),
            ObjectExpressionSyntax objectExpression => BindObjectExpression(objectExpression),
            ArrayExpressionSyntax arrayExpression => BindArrayExpression(arrayExpression),
            LambdaExpressionSyntax lambdaExpression => BindLambdaExpression(lambdaExpression),
            _ => throw new InvalidOperationException($"Unsupported expression syntax '{expression.Kind}'.")
        };
    }

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax literal)
    {
        var value = literal.Value;
        var type = value switch
        {
            null => TypeSymbol.Nil,
            long => TypeSymbol.Int,
            double => TypeSymbol.Double,
            bool => TypeSymbol.Bool,
            char => TypeSymbol.Char,
            string => TypeSymbol.String,
            _ => TypeSymbol.Error
        };

        return new BoundLiteralExpression(value, type);
    }

    private BoundExpression BindInjectExpression(InjectExpressionSyntax injectExpression)
    {
        var path = injectExpression.PathToken.Value?.ToString() ?? string.Empty;
        var resolvedPath = ResolvePath(path);
        var injectedModel = SemanticModel.Create(SourceFile.Load(new FileInfo(resolvedPath)));

        foreach (var diagnostic in injectedModel.Diagnostics)
        {
            _diagnostics.Add(diagnostic);
        }

        var propertyTypes = new Dictionary<string, TypeSymbol>(StringComparer.Ordinal);
        foreach (var variable in injectedModel.PublicVariables)
        {
            propertyTypes[variable.Name] = variable.Type;
        }

        foreach (var function in injectedModel.PublicFunctions)
        {
            propertyTypes[function.Name] = function.Type;
        }

        return new BoundInjectExpression(path, new ObjectTypeSymbol(propertyTypes));
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax name)
    {
        if (_scope.TryLookupVariable(name.IdentifierToken.Text, out var variable))
        {
            return new BoundNameExpression(variable, variable.Type);
        }

        if (_scope.TryLookupFunction(name.IdentifierToken.Text, out var function))
        {
            return new BoundNameExpression(function, function.Type);
        }

        Report(name.Span, $"Undefined name '{name.IdentifierToken.Text}'.");
        return new BoundErrorExpression();
    }

    private BoundExpression BindVariableDeclarationExpression(VariableDeclarationExpressionSyntax declaration)
    {
        var initializer = BindExpression(declaration.Initializer);
        var variable = new VariableSymbol(
            declaration.IdentifierToken.Text,
            initializer.Type,
            isPublic: declaration.PublicKeyword is not null);

        if (!_scope.TryDeclareVariable(variable))
        {
            Report(declaration.IdentifierToken.Span, $"A variable named '{variable.Name}' is already declared in this scope.");
        }

        return new BoundVariableDeclarationExpression(variable, initializer);
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax assignment)
    {
        var boundExpression = BindExpression(assignment.Expression);

        if (assignment.Target is NameExpressionSyntax name)
        {
            if (!_scope.TryLookupVariable(name.IdentifierToken.Text, out var variable))
            {
                Report(name.IdentifierToken.Span, $"Undefined variable '{name.IdentifierToken.Text}'.");
                return new BoundErrorExpression();
            }

            if (!IsAssignable(variable.Type, boundExpression.Type))
            {
                Report(assignment.Expression.Span, $"Cannot assign a value of type '{boundExpression.Type.Name}' to '{variable.Type.Name}'.");
            }

            return new BoundAssignmentExpression(variable, boundExpression);
        }

        if (assignment.Target is MemberAccessExpressionSyntax memberAccess)
        {
            var target = BindExpression(memberAccess.Target);
            if (target.Type == TypeSymbol.Error)
            {
                return new BoundErrorExpression();
            }

            if (target.Type == TypeSymbol.Any)
            {
                return new BoundMemberAssignmentExpression(target, memberAccess.IdentifierToken.Text, boundExpression, TypeSymbol.Any);
            }

            if (target.Type is not ObjectTypeSymbol objectType)
            {
                Report(memberAccess.Target.Span, $"Type '{target.Type.Name}' does not support member assignment.");
                return new BoundErrorExpression();
            }

            var resultType = boundExpression.Type;

            return new BoundMemberAssignmentExpression(target, memberAccess.IdentifierToken.Text, boundExpression, resultType);
        }

        Report(assignment.Target.Span, "Invalid assignment target.");
        return new BoundErrorExpression();
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax unary)
    {
        var operand = BindExpression(unary.Operand);
        var resultType = unary.OperatorToken.Kind switch
        {
            SyntaxKind.PlusToken or SyntaxKind.MinusToken when IsNumber(operand.Type) => operand.Type,
            SyntaxKind.BangToken when IsCompatible(operand.Type, TypeSymbol.Bool) => TypeSymbol.Bool,
            SyntaxKind.TildeToken when IsCompatible(operand.Type, TypeSymbol.Int) => TypeSymbol.Int,
            _ => TypeSymbol.Error
        };

        if (resultType == TypeSymbol.Error)
        {
            Report(unary.Span, $"Operator '{unary.OperatorToken.Text}' is not defined for type '{operand.Type.Name}'.");
        }

        return new BoundUnaryExpression(unary.OperatorToken.Kind, operand, resultType);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax binary)
    {
        var left = BindExpression(binary.Left);
        var right = BindExpression(binary.Right);
        var resultType = BindBinaryOperator(binary.OperatorToken.Kind, left.Type, right.Type);

        if (resultType == TypeSymbol.Error)
        {
            Report(binary.Span, $"Operator '{binary.OperatorToken.Text}' is not defined for '{left.Type.Name}' and '{right.Type.Name}'.");
        }

        return new BoundBinaryExpression(left, binary.OperatorToken.Kind, right, resultType);
    }

    private BoundExpression BindConditionalExpression(ConditionalExpressionSyntax conditional)
    {
        var condition = BindExpression(conditional.Condition);
        RequireType(condition.Type, TypeSymbol.Bool, conditional.Condition.Span, "Conditional expressions require a bool condition.");

        var whenTrue = BindExpression(conditional.WhenTrue);
        var whenFalse = BindExpression(conditional.WhenFalse);
        var resultType = GetCommonType(whenTrue.Type, whenFalse.Type);

        return new BoundConditionalExpression(condition, whenTrue, whenFalse, resultType);
    }

    private BoundExpression BindCallExpression(CallExpressionSyntax call)
    {
        var target = BindExpression(call.Target);
        var arguments = call.Arguments.Select(BindExpression).ToArray();

        if (target.Type is not FunctionTypeSymbol functionType)
        {
            Report(call.Target.Span, "Only function values can be called.");
            return new BoundCallExpression(target, arguments, TypeSymbol.Error);
        }

        if (arguments.Length != functionType.ParameterTypes.Count)
        {
            Report(call.Span, $"Function expects {functionType.ParameterTypes.Count} argument(s) but received {arguments.Length}.");
        }

        for (var i = 0; i < Math.Min(arguments.Length, functionType.ParameterTypes.Count); i++)
        {
            if (!IsAssignable(functionType.ParameterTypes[i], arguments[i].Type))
            {
                Report(call.Arguments[i].Span, $"Argument {i + 1} cannot convert from '{arguments[i].Type.Name}' to '{functionType.ParameterTypes[i].Name}'.");
            }
        }

        return new BoundCallExpression(target, arguments, functionType.ReturnType);
    }

    private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax memberAccess)
    {
        var target = BindExpression(memberAccess.Target);
        if (target.Type == TypeSymbol.Any)
        {
            return new BoundMemberAccessExpression(target, memberAccess.IdentifierToken.Text, TypeSymbol.Any);
        }

        if (target.Type == TypeSymbol.String)
        {
            TypeSymbol memberType = memberAccess.IdentifierToken.Text switch
            {
                "len" => TypeSymbol.Int,
                "at" => new FunctionTypeSymbol([TypeSymbol.Int], TypeSymbol.Char),
                "add" => new FunctionTypeSymbol([TypeSymbol.Any], TypeSymbol.String),
                "insert" => new FunctionTypeSymbol([TypeSymbol.Int, TypeSymbol.Any], TypeSymbol.String),
                "replace" => new FunctionTypeSymbol([TypeSymbol.Int, TypeSymbol.Any], TypeSymbol.String),
                "remove" => new FunctionTypeSymbol([TypeSymbol.Int], TypeSymbol.String),
                "forEach" => new FunctionTypeSymbol(
                    [new FunctionTypeSymbol([TypeSymbol.Char], TypeSymbol.Void)],
                    TypeSymbol.Void),
                _ => TypeSymbol.Error
            };

            if (memberType != TypeSymbol.Error)
            {
                return new BoundMemberAccessExpression(target, memberAccess.IdentifierToken.Text, memberType);
            }
        }

        if (target.Type is ArrayTypeSymbol arrayType)
        {
            TypeSymbol memberType = memberAccess.IdentifierToken.Text switch
            {
                "len" => TypeSymbol.Int,
                "at" => new FunctionTypeSymbol([TypeSymbol.Int], arrayType.ElementType),
                "add" => new FunctionTypeSymbol([arrayType.ElementType], TypeSymbol.Void),
                "insert" => new FunctionTypeSymbol([TypeSymbol.Int, arrayType.ElementType], TypeSymbol.Void),
                "replace" => new FunctionTypeSymbol([TypeSymbol.Int, arrayType.ElementType], arrayType.ElementType),
                "remove" => new FunctionTypeSymbol([TypeSymbol.Int], arrayType.ElementType),
                "forEach" => new FunctionTypeSymbol(
                    [new FunctionTypeSymbol([arrayType.ElementType], TypeSymbol.Void)],
                    TypeSymbol.Void),
                _ => TypeSymbol.Error
            };

            if (memberType != TypeSymbol.Error)
            {
                return new BoundMemberAccessExpression(target, memberAccess.IdentifierToken.Text, memberType);
            }
        }

        if (target.Type is ObjectTypeSymbol objectType &&
            objectType.TryGetProperty(memberAccess.IdentifierToken.Text, out var propertyType))
        {
            return new BoundMemberAccessExpression(target, memberAccess.IdentifierToken.Text, propertyType);
        }

        if (target.Type is ObjectTypeSymbol dynamicObjectType && memberAccess.IdentifierToken.Text == "get")
        {
            var returnType = dynamicObjectType.Properties.Count == 0
                ? TypeSymbol.Any
                : dynamicObjectType.Properties.Values.Aggregate(GetCommonType);

            return new BoundMemberAccessExpression(
                target,
                memberAccess.IdentifierToken.Text,
                new FunctionTypeSymbol([TypeSymbol.String], returnType));
        }

        if (target.Type is ObjectTypeSymbol objectMemberType)
        {
            var valueType = objectMemberType.Properties.Count == 0
                ? TypeSymbol.Any
                : objectMemberType.Properties.Values.Aggregate(GetCommonType);

            TypeSymbol memberType = memberAccess.IdentifierToken.Text switch
            {
                "len" => TypeSymbol.Int,
                "add" => new FunctionTypeSymbol([TypeSymbol.String, TypeSymbol.Any], TypeSymbol.Void),
                "remove" => new FunctionTypeSymbol([TypeSymbol.String], valueType),
                "forEach" => new FunctionTypeSymbol(
                    [new FunctionTypeSymbol([TypeSymbol.String, valueType], TypeSymbol.Void)],
                    TypeSymbol.Void),
                _ => TypeSymbol.Error
            };

            if (memberType != TypeSymbol.Error)
            {
                return new BoundMemberAccessExpression(target, memberAccess.IdentifierToken.Text, memberType);
            }
        }

        Report(memberAccess.IdentifierToken.Span, $"Type '{target.Type.Name}' does not contain a member named '{memberAccess.IdentifierToken.Text}'.");
        return new BoundMemberAccessExpression(target, memberAccess.IdentifierToken.Text, TypeSymbol.Error);
    }

    private BoundExpression BindObjectExpression(ObjectExpressionSyntax objectExpression)
    {
        var properties = new List<BoundObjectProperty>();
        var propertyTypes = new Dictionary<string, TypeSymbol>(StringComparer.Ordinal);

        foreach (var property in objectExpression.Properties)
        {
            var value = BindExpression(property.Value);
            if (!propertyTypes.TryAdd(property.IdentifierToken.Text, value.Type))
            {
                Report(property.IdentifierToken.Span, $"Duplicate object property '{property.IdentifierToken.Text}'.");
            }

            properties.Add(new BoundObjectProperty(property.IdentifierToken.Text, value));
        }

        return new BoundObjectExpression(properties, new ObjectTypeSymbol(propertyTypes));
    }

    private BoundExpression BindArrayExpression(ArrayExpressionSyntax arrayExpression)
    {
        var elements = arrayExpression.Elements.Select(BindExpression).ToArray();
        var elementType = elements.Length == 0
            ? TypeSymbol.Any
            : elements.Select(static element => element.Type).Aggregate(GetCommonType);

        return new BoundArrayExpression(elements, new ArrayTypeSymbol(elementType));
    }

    private BoundExpression BindLambdaExpression(LambdaExpressionSyntax lambdaExpression)
    {
        var parameters = lambdaExpression.Parameters
            .Select(parameter => new VariableSymbol(parameter.IdentifierToken.Text, TypeSymbol.Any, isParameter: true))
            .ToArray();

        var body = BindCallableBody(parameters, lambdaExpression.Body, out var returnType);
        var type = new FunctionTypeSymbol([.. parameters.Select(static parameter => parameter.Type)], returnType);

        return new BoundLambdaExpression(parameters, body, type);
    }

    private BoundBlockStatement BindCallableBody(
        IReadOnlyList<VariableSymbol> parameters,
        BlockStatementSyntax body,
        out TypeSymbol returnType)
    {
        var previousScope = _scope;
        var previousReturnTypes = _currentReturnTypes;
        var previousLoopDepth = _loopDepth;

        _scope = new BoundScope(previousScope);
        _currentReturnTypes = [];
        _loopDepth = 0;

        foreach (var parameter in parameters)
        {
            if (!_scope.TryDeclareVariable(parameter))
            {
                Report(body.Span, $"Duplicate parameter '{parameter.Name}'.");
            }
        }

        PredeclareFunctions(body.Statements);

        var statements = new List<BoundStatement>();
        foreach (var statement in body.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        returnType = InferReturnType(_currentReturnTypes);
        _scope = previousScope;
        _currentReturnTypes = previousReturnTypes;
        _loopDepth = previousLoopDepth;

        return new BoundBlockStatement(statements);
    }

    private void PredeclareFunctions(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is not FunctionDeclarationStatementSyntax functionDeclaration)
            {
                continue;
            }

            var parameters = functionDeclaration.Parameters
                .Select(parameter => new VariableSymbol(parameter.IdentifierToken.Text, TypeSymbol.Any, isParameter: true))
                .ToArray();

            var function = new FunctionSymbol(
                functionDeclaration.IdentifierToken.Text,
                parameters,
                TypeSymbol.Any,
                isPublic: functionDeclaration.PublicKeyword is not null);
            if (!_scope.TryDeclareFunction(function))
            {
                Report(functionDeclaration.IdentifierToken.Span, $"A function named '{function.Name}' is already declared in this scope.");
            }
        }
    }

    private void DeclareBuiltInVariables()
    {
        foreach (var builtInVariable in BuiltInVariables.All)
        {
            _scope.TryDeclareVariable(builtInVariable);
        }
    }

    private void RequireType(TypeSymbol actualType, TypeSymbol expectedType, TextSpan span, string message)
    {
        if (!IsAssignable(expectedType, actualType))
        {
            Report(span, message);
        }
    }

    private bool IsAssignable(TypeSymbol targetType, TypeSymbol sourceType)
    {
        if (targetType == TypeSymbol.Error || sourceType == TypeSymbol.Error)
        {
            return true;
        }

        if (targetType == TypeSymbol.Any || sourceType == TypeSymbol.Any)
        {
            return true;
        }

        if (sourceType == TypeSymbol.Nil)
        {
            return targetType == TypeSymbol.Nil ||
                   targetType == TypeSymbol.String ||
                   targetType is ArrayTypeSymbol or ObjectTypeSymbol or FunctionTypeSymbol;
        }

        if (targetType.Equals(sourceType))
        {
            return true;
        }

        if (targetType is FunctionTypeSymbol targetFunction && sourceType is FunctionTypeSymbol sourceFunction)
        {
            if (targetFunction.ParameterTypes.Count != sourceFunction.ParameterTypes.Count)
            {
                return false;
            }

            for (var i = 0; i < targetFunction.ParameterTypes.Count; i++)
            {
                if (!IsAssignable(sourceFunction.ParameterTypes[i], targetFunction.ParameterTypes[i]))
                {
                    return false;
                }
            }

            return IsAssignable(targetFunction.ReturnType, sourceFunction.ReturnType);
        }

        return targetType == TypeSymbol.Double && sourceType == TypeSymbol.Int;
    }

    private static bool IsCompatible(TypeSymbol left, TypeSymbol right)
    {
        return left == TypeSymbol.Any ||
               right == TypeSymbol.Any ||
               left.Equals(right);
    }

    private static bool IsNumber(TypeSymbol type)
    {
        return type == TypeSymbol.Int || type == TypeSymbol.Double || type == TypeSymbol.Any;
    }

    private static TypeSymbol GetCommonType(TypeSymbol left, TypeSymbol right)
    {
        if (left == TypeSymbol.Error)
        {
            return right;
        }

        if (right == TypeSymbol.Error)
        {
            return left;
        }

        if (left.Equals(right))
        {
            return left;
        }

        if (left == TypeSymbol.Nil)
        {
            return right;
        }

        if (right == TypeSymbol.Nil)
        {
            return left;
        }

        if (left == TypeSymbol.Any || right == TypeSymbol.Any)
        {
            return TypeSymbol.Any;
        }

        if ((left == TypeSymbol.Int && right == TypeSymbol.Double) ||
            (left == TypeSymbol.Double && right == TypeSymbol.Int))
        {
            return TypeSymbol.Double;
        }

        return TypeSymbol.Any;
    }

    private static TypeSymbol InferReturnType(IReadOnlyList<TypeSymbol> returnTypes)
    {
        if (returnTypes.Count == 0)
        {
            return TypeSymbol.Void;
        }

        return returnTypes.Aggregate(GetCommonType);
    }

    private TypeSymbol BindBinaryOperator(SyntaxKind operatorKind, TypeSymbol leftType, TypeSymbol rightType)
    {
        if (leftType == TypeSymbol.Error || rightType == TypeSymbol.Error)
        {
            return TypeSymbol.Error;
        }

        if (operatorKind == SyntaxKind.PlusToken &&
            (leftType == TypeSymbol.String || rightType == TypeSymbol.String))
        {
            return TypeSymbol.String;
        }

        if (IsNumber(leftType) && IsNumber(rightType))
        {
            return operatorKind switch
            {
                SyntaxKind.PlusToken or
                SyntaxKind.MinusToken or
                SyntaxKind.StarToken or
                SyntaxKind.SlashToken or
                SyntaxKind.PercentToken => GetCommonType(leftType, rightType),
                SyntaxKind.LessLessToken or
                SyntaxKind.GreaterGreaterToken => TypeSymbol.Int,
                SyntaxKind.LessToken or
                SyntaxKind.LessEqualsToken or
                SyntaxKind.GreaterToken or
                SyntaxKind.GreaterEqualsToken or
                SyntaxKind.EqualsEqualsToken or
                SyntaxKind.BangEqualsToken => TypeSymbol.Bool,
                SyntaxKind.AmpersandToken or
                SyntaxKind.PipeToken or
                SyntaxKind.CaretToken when leftType != TypeSymbol.Double && rightType != TypeSymbol.Double => TypeSymbol.Int,
                _ => TypeSymbol.Error
            };
        }

        if (IsCompatible(leftType, TypeSymbol.Bool) && IsCompatible(rightType, TypeSymbol.Bool))
        {
            return operatorKind switch
            {
                SyntaxKind.AmpersandAmpersandToken or
                SyntaxKind.PipePipeToken or
                SyntaxKind.EqualsEqualsToken or
                SyntaxKind.BangEqualsToken => TypeSymbol.Bool,
                _ => TypeSymbol.Error
            };
        }

        if (operatorKind is SyntaxKind.EqualsEqualsToken or SyntaxKind.BangEqualsToken &&
            (IsAssignable(leftType, rightType) || IsAssignable(rightType, leftType)))
        {
            return TypeSymbol.Bool;
        }

        return TypeSymbol.Error;
    }

    private void Report(TextSpan span, string message)
    {
        _diagnostics.Add(new Diagnostic(
            message,
            _parseResult.SourceFile.GetLocation(span),
            DiagnosticLevel.Error));
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var baseDirectory = _parseResult.SourceFile.Info?.DirectoryName ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new(StringComparer.Ordinal);
        private readonly Dictionary<string, FunctionSymbol> _functions = new(StringComparer.Ordinal);

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public BoundScope? Parent { get; }

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            return _variables.TryAdd(variable.Name, variable);
        }

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            return _functions.TryAdd(function.Name, function);
        }

        public bool TryLookupVariable(string name, out VariableSymbol variable)
        {
            for (var scope = this; scope is not null; scope = scope.Parent)
            {
                if (scope._variables.TryGetValue(name, out variable!))
                {
                    return true;
                }
            }

            variable = null!;
            return false;
        }

        public bool TryLookupFunction(string name, out FunctionSymbol function)
        {
            for (var scope = this; scope is not null; scope = scope.Parent)
            {
                if (scope._functions.TryGetValue(name, out function!))
                {
                    return true;
                }
            }

            function = null!;
            return false;
        }

        public IReadOnlyList<VariableSymbol> GetDeclaredVariables() => [.. _variables.Values];
        public IReadOnlyList<FunctionSymbol> GetDeclaredFunctions() => [.. _functions.Values];
    }
}
