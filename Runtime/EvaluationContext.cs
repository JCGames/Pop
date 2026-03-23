using Pop.Language;

namespace Pop.Runtime;

internal sealed class EvaluationContext
{
    private readonly TextWriter _output;
    private readonly TextReader _input;

    public EvaluationContext(TextWriter output, TextReader input)
    {
        _output = output;
        _input = input;
    }

    public RuntimeEnvironment CreateGlobalEnvironment()
    {
        var environment = new RuntimeEnvironment(null);
        DeclareBuiltIns(environment);
        return environment;
    }

    public ExecutionSignal ExecuteCompilationUnit(BoundCompilationUnit root, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        foreach (var statement in root.Statements)
        {
            var signal = ExecuteStatement(statement, environment, sourceFile);
            if (signal.Kind != ExecutionSignalKind.None)
            {
                return signal;
            }
        }

        return ExecutionSignal.None;
    }

    public ExecutionSignal ExecuteBlock(BoundBlockStatement block, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        foreach (var statement in block.Statements)
        {
            var signal = ExecuteStatement(statement, environment, sourceFile);
            if (signal.Kind != ExecutionSignalKind.None)
            {
                return signal;
            }
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteStatement(BoundStatement statement, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        switch (statement)
        {
            case BoundBlockStatement block:
                return ExecuteBlock(block, new RuntimeEnvironment(environment), sourceFile);
            case BoundExpressionStatement expressionStatement:
                EvaluateExpression(expressionStatement.Expression, environment, sourceFile);
                return ExecutionSignal.None;
            case BoundWhileStatement whileStatement:
                return ExecuteWhileStatement(whileStatement, environment, sourceFile);
            case BoundIfStatement ifStatement:
                return ExecuteIfStatement(ifStatement, environment, sourceFile);
            case BoundReturnStatement returnStatement:
                return new ExecutionSignal(ExecutionSignalKind.Return, returnStatement.Expression is null ? null : EvaluateExpression(returnStatement.Expression, environment, sourceFile));
            case BoundContinueStatement:
                return new ExecutionSignal(ExecutionSignalKind.Continue, null);
            case BoundBreakStatement:
                return new ExecutionSignal(ExecutionSignalKind.Break, null);
            case BoundFunctionDeclarationStatement functionDeclaration:
                environment.DeclareFunction(
                    functionDeclaration.Symbol,
                    new UserFunctionCallable(functionDeclaration.Symbol, functionDeclaration.Body, environment, sourceFile));
                return ExecutionSignal.None;
            default:
                throw new InvalidOperationException($"Unsupported bound statement '{statement.Kind}'.");
        }
    }

    private ExecutionSignal ExecuteWhileStatement(BoundWhileStatement whileStatement, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        while (IsTrue(EvaluateExpression(whileStatement.Condition, environment, sourceFile)))
        {
            var signal = ExecuteBlock(whileStatement.Body, new RuntimeEnvironment(environment), sourceFile);
            switch (signal.Kind)
            {
                case ExecutionSignalKind.None:
                case ExecutionSignalKind.Continue:
                    continue;
                case ExecutionSignalKind.Break:
                    return ExecutionSignal.None;
                case ExecutionSignalKind.Return:
                    return signal;
                default:
                    throw new InvalidOperationException($"Unexpected execution signal '{signal.Kind}'.");
            }
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteIfStatement(BoundIfStatement ifStatement, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        if (IsTrue(EvaluateExpression(ifStatement.Condition, environment, sourceFile)))
        {
            return ExecuteBlock(ifStatement.ThenStatement, new RuntimeEnvironment(environment), sourceFile);
        }

        return ifStatement.ElseStatement is null
            ? ExecutionSignal.None
            : ExecuteStatement(ifStatement.ElseStatement, new RuntimeEnvironment(environment), sourceFile);
    }

    private object? EvaluateExpression(BoundExpression expression, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        return expression switch
        {
            BoundErrorExpression => null,
            BoundLiteralExpression literal => literal.Value,
            BoundInjectExpression injectExpression => EvaluateInjectExpression(injectExpression, sourceFile),
            BoundNameExpression name => EvaluateNameExpression(name, environment),
            BoundVariableDeclarationExpression declaration => EvaluateVariableDeclarationExpression(declaration, environment, sourceFile),
            BoundAssignmentExpression assignment => EvaluateAssignmentExpression(assignment, environment, sourceFile),
            BoundUnaryExpression unary => EvaluateUnaryExpression(unary, environment, sourceFile),
            BoundBinaryExpression binary => EvaluateBinaryExpression(binary, environment, sourceFile),
            BoundConditionalExpression conditional => EvaluateConditionalExpression(conditional, environment, sourceFile),
            BoundCallExpression call => EvaluateCallExpression(call, environment, sourceFile),
            BoundMemberAccessExpression memberAccess => EvaluateMemberAccessExpression(memberAccess, environment, sourceFile),
            BoundObjectExpression objectExpression => EvaluateObjectExpression(objectExpression, environment, sourceFile),
            BoundArrayExpression arrayExpression => EvaluateArrayExpression(arrayExpression, environment, sourceFile),
            BoundLambdaExpression lambdaExpression => new LambdaCallable(lambdaExpression.Parameters, lambdaExpression.Body, environment, sourceFile),
            _ => throw new InvalidOperationException($"Unsupported bound expression '{expression.Kind}'.")
        };
    }

    private object EvaluateInjectExpression(BoundInjectExpression injectExpression, SourceFile sourceFile)
    {
        var resolvedPath = ResolvePath(sourceFile, injectExpression.Path);
        var injectedFile = SourceFile.Load(new FileInfo(resolvedPath));
        var model = SemanticModel.Create(injectedFile);
        if (model.Diagnostics.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, model.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        var moduleEnvironment = CreateGlobalEnvironment();
        ExecuteCompilationUnit(model.Root, moduleEnvironment, injectedFile);

        var exports = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var variable in model.PublicVariables)
        {
            if (moduleEnvironment.TryGetVariable(variable, out var value))
            {
                exports[variable.Name] = value;
            }
        }

        foreach (var function in model.PublicFunctions)
        {
            if (moduleEnvironment.TryGetFunction(function, out var callable))
            {
                exports[function.Name] = callable;
            }
        }

        return exports;
    }

    private void DeclareBuiltIns(RuntimeEnvironment environment)
    {
        var corn = new Dictionary<string, object?>(StringComparer.Ordinal);

        RegisterBuiltIn(corn, BuiltInSymbols.Print, new BuiltInCallable((_, arguments) =>
        {
            _output.WriteLine(RuntimeValueFormatter.Format(arguments[0]));
            return null;
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Len, new BuiltInCallable((_, arguments) => arguments[0] switch
        {
            string text => (long)text.Length,
            IReadOnlyCollection<object?> values => (long)values.Count,
            IReadOnlyDictionary<string, object?> properties => (long)properties.Count,
            _ => throw new InvalidOperationException("len expects a string, array, or object.")
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Type, new BuiltInCallable((_, arguments) => GetTypeName(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.Str, new BuiltInCallable((_, arguments) => RuntimeValueFormatter.Format(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.Int, new BuiltInCallable((_, arguments) => ConvertToInt(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.Double, new BuiltInCallable((_, arguments) => ConvertToDouble(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.Bool, new BuiltInCallable((_, arguments) => ConvertToBool(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.Input, new BuiltInCallable((_, _) => _input.ReadLine() ?? string.Empty));

        RegisterBuiltIn(corn, BuiltInSymbols.Keys, new BuiltInCallable((_, arguments) => arguments[0] switch
        {
            IReadOnlyDictionary<string, object?> properties => properties.Keys.Cast<object?>().ToList(),
            _ => throw new InvalidOperationException("keys expects an object.")
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Has, new BuiltInCallable((_, arguments) => arguments[0] switch
        {
            IReadOnlyDictionary<string, object?> properties => properties.ContainsKey(Convert.ToString(arguments[1]) ?? string.Empty),
            _ => throw new InvalidOperationException("has expects an object as its first argument.")
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Push, new BuiltInCallable((_, arguments) =>
        {
            if (arguments[0] is not List<object?> array)
            {
                throw new InvalidOperationException("push expects an array.");
            }

            array.Add(arguments[1]);
            return null;
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Pop, new BuiltInCallable((_, arguments) =>
        {
            if (arguments[0] is not List<object?> array)
            {
                throw new InvalidOperationException("pop expects an array.");
            }

            if (array.Count == 0)
            {
                return null;
            }

            var index = array.Count - 1;
            var value = array[index];
            array.RemoveAt(index);
            return value;
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Clock, new BuiltInCallable((_, _) =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d));

        RegisterBuiltIn(corn, BuiltInSymbols.Read, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return File.ReadAllText(path);
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Write, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            var text = Convert.ToString(arguments[1]) ?? string.Empty;
            File.WriteAllText(path, text);
            return null;
        }));

        environment.DeclareVariable(BuiltInVariables.Corn, corn);
    }

    private static void RegisterBuiltIn(
        IDictionary<string, object?> corn,
        FunctionSymbol symbol,
        IRuntimeCallable callable)
    {
        corn[symbol.Name] = callable;
    }

    private object? EvaluateNameExpression(BoundNameExpression name, RuntimeEnvironment environment)
    {
        return name.Symbol switch
        {
            VariableSymbol variable when environment.TryGetVariable(variable, out var value) => value,
            FunctionSymbol function when environment.TryGetFunction(function, out var callable) => callable,
            _ => throw new InvalidOperationException($"Undefined runtime symbol '{name.Symbol.Name}'.")
        };
    }

    private object? EvaluateVariableDeclarationExpression(BoundVariableDeclarationExpression declaration, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var value = EvaluateExpression(declaration.Initializer, environment, sourceFile);
        environment.DeclareVariable(declaration.Variable, value);
        return value;
    }

    private object? EvaluateAssignmentExpression(BoundAssignmentExpression assignment, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var value = EvaluateExpression(assignment.Expression, environment, sourceFile);
        if (!environment.TryAssignVariable(assignment.Variable, value))
        {
            throw new InvalidOperationException($"Undefined runtime variable '{assignment.Variable.Name}'.");
        }

        return value;
    }

    private object? EvaluateUnaryExpression(BoundUnaryExpression unary, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var operand = EvaluateExpression(unary.Operand, environment, sourceFile);
        return unary.OperatorKind switch
        {
            SyntaxKind.PlusToken => operand,
            SyntaxKind.MinusToken when operand is long integer => -integer,
            SyntaxKind.MinusToken when operand is double floating => -floating,
            SyntaxKind.BangToken when operand is bool boolean => !boolean,
            SyntaxKind.TildeToken when operand is long integer => ~integer,
            _ => throw new InvalidOperationException($"Unsupported unary operation '{unary.OperatorKind}'.")
        };
    }

    private object? EvaluateBinaryExpression(BoundBinaryExpression binary, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var left = EvaluateExpression(binary.Left, environment, sourceFile);
        var right = EvaluateExpression(binary.Right, environment, sourceFile);

        return binary.OperatorKind switch
        {
            SyntaxKind.PlusToken => EvaluateAddition(left, right),
            SyntaxKind.MinusToken => EvaluateNumeric(left, right, static (l, r) => l - r, static (l, r) => l - r),
            SyntaxKind.StarToken => EvaluateNumeric(left, right, static (l, r) => l * r, static (l, r) => l * r),
            SyntaxKind.SlashToken => EvaluateNumeric(left, right, static (l, r) => l / r, static (l, r) => l / r),
            SyntaxKind.PercentToken => EvaluateNumeric(left, right, static (l, r) => l % r, static (l, r) => l % r),
            SyntaxKind.LessToken => EvaluateComparison(left, right, static (l, r) => l < r, static (l, r) => l < r),
            SyntaxKind.LessEqualsToken => EvaluateComparison(left, right, static (l, r) => l <= r, static (l, r) => l <= r),
            SyntaxKind.GreaterToken => EvaluateComparison(left, right, static (l, r) => l > r, static (l, r) => l > r),
            SyntaxKind.GreaterEqualsToken => EvaluateComparison(left, right, static (l, r) => l >= r, static (l, r) => l >= r),
            SyntaxKind.EqualsEqualsToken => Equals(left, right),
            SyntaxKind.BangEqualsToken => !Equals(left, right),
            SyntaxKind.AmpersandAmpersandToken => IsTrue(left) && IsTrue(right),
            SyntaxKind.PipePipeToken => IsTrue(left) || IsTrue(right),
            SyntaxKind.AmpersandToken when left is long leftInt && right is long rightInt => leftInt & rightInt,
            SyntaxKind.PipeToken when left is long leftInt && right is long rightInt => leftInt | rightInt,
            SyntaxKind.CaretToken when left is long leftInt && right is long rightInt => leftInt ^ rightInt,
            SyntaxKind.LessLessToken when left is long leftInt && right is long rightInt => leftInt << (int)rightInt,
            SyntaxKind.GreaterGreaterToken when left is long leftInt && right is long rightInt => leftInt >> (int)rightInt,
            _ => throw new InvalidOperationException($"Unsupported binary operation '{binary.OperatorKind}'.")
        };
    }

    private object? EvaluateConditionalExpression(BoundConditionalExpression conditional, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        return IsTrue(EvaluateExpression(conditional.Condition, environment, sourceFile))
            ? EvaluateExpression(conditional.WhenTrue, environment, sourceFile)
            : EvaluateExpression(conditional.WhenFalse, environment, sourceFile);
    }

    private object? EvaluateCallExpression(BoundCallExpression call, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var target = EvaluateExpression(call.Target, environment, sourceFile);
        if (target is not IRuntimeCallable callable)
        {
            throw new InvalidOperationException("Call target is not callable.");
        }

        var arguments = call.Arguments.Select(argument => EvaluateExpression(argument, environment, sourceFile)).ToArray();
        return callable.Invoke(this, arguments);
    }

    private object? EvaluateMemberAccessExpression(BoundMemberAccessExpression memberAccess, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var target = EvaluateExpression(memberAccess.Target, environment, sourceFile);
        if (target is List<object?> array)
        {
            return memberAccess.MemberName switch
            {
                "len" => (long)array.Count,
                "at" => new ArrayAtCallable(array),
                "forEach" => new ArrayForEachCallable(array),
                _ => throw new InvalidOperationException($"Array does not contain member '{memberAccess.MemberName}'.")
            };
        }

        if (target is IReadOnlyDictionary<string, object?> properties &&
            properties.TryGetValue(memberAccess.MemberName, out var value))
        {
            return value;
        }

        if (target is IReadOnlyDictionary<string, object?> objectProperties && memberAccess.MemberName == "get")
        {
            return new ObjectGetCallable(objectProperties);
        }

        throw new InvalidOperationException($"Object does not contain member '{memberAccess.MemberName}'.");
    }

    private object EvaluateObjectExpression(BoundObjectExpression objectExpression, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in objectExpression.Properties)
        {
            values[property.Name] = EvaluateExpression(property.Value, environment, sourceFile);
        }

        return values;
    }

    private object EvaluateArrayExpression(BoundArrayExpression arrayExpression, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        return arrayExpression.Elements
            .Select(element => EvaluateExpression(element, environment, sourceFile))
            .ToList();
    }

    private static object EvaluateAddition(object? left, object? right)
    {
        if (left is string || right is string)
        {
            return RuntimeValueFormatter.Format(left) + RuntimeValueFormatter.Format(right);
        }

        return EvaluateNumeric(left, right, static (l, r) => l + r, static (l, r) => l + r);
    }

    private static object EvaluateNumeric(
        object? left,
        object? right,
        Func<long, long, long> integerOperation,
        Func<double, double, double> floatingOperation)
    {
        if (left is double || right is double)
        {
            return floatingOperation(Convert.ToDouble(left), Convert.ToDouble(right));
        }

        return integerOperation(Convert.ToInt64(left), Convert.ToInt64(right));
    }

    private static bool EvaluateComparison(
        object? left,
        object? right,
        Func<long, long, bool> integerOperation,
        Func<double, double, bool> floatingOperation)
    {
        if (left is double || right is double)
        {
            return floatingOperation(Convert.ToDouble(left), Convert.ToDouble(right));
        }

        return integerOperation(Convert.ToInt64(left), Convert.ToInt64(right));
    }

    private static bool IsTrue(object? value)
    {
        return value is true;
    }

    private static long ConvertToInt(object? value)
    {
        return value switch
        {
            null => 0,
            long integer => integer,
            double floating => (long)floating,
            bool boolean => boolean ? 1 : 0,
            char character => character,
            string text when long.TryParse(text, out var parsed) => parsed,
            string text when double.TryParse(text, out var floating) => (long)floating,
            _ => throw new InvalidOperationException("int conversion failed.")
        };
    }

    private static double ConvertToDouble(object? value)
    {
        return value switch
        {
            null => 0d,
            long integer => integer,
            double floating => floating,
            bool boolean => boolean ? 1d : 0d,
            char character => character,
            string text when double.TryParse(text, out var parsed) => parsed,
            _ => throw new InvalidOperationException("double conversion failed.")
        };
    }

    private static bool ConvertToBool(object? value)
    {
        return value switch
        {
            null => false,
            bool boolean => boolean,
            long integer => integer != 0,
            double floating => floating != 0,
            char character => character != '\0',
            string text => !string.IsNullOrEmpty(text),
            IReadOnlyCollection<object?> values => values.Count > 0,
            IReadOnlyDictionary<string, object?> properties => properties.Count > 0,
            IRuntimeCallable => true,
            _ => true
        };
    }

    private static string GetTypeName(object? value)
    {
        return value switch
        {
            null => "null",
            long => "int",
            double => "double",
            bool => "bool",
            char => "char",
            string => "string",
            IReadOnlyDictionary<string, object?> => "object",
            IEnumerable<object?> => "array",
            IRuntimeCallable => "function",
            _ => "unknown"
        };
    }

    private static string ResolvePath(SourceFile sourceFile, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var baseDirectory = sourceFile.Info?.DirectoryName ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static string ResolvePath(object? pathValue)
    {
        var path = Convert.ToString(pathValue) ?? string.Empty;
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(path);
    }
}

internal readonly record struct ExecutionSignal(ExecutionSignalKind Kind, object? Value)
{
    public static ExecutionSignal None { get; } = new(ExecutionSignalKind.None, null);
}

internal enum ExecutionSignalKind
{
    None,
    Return,
    Continue,
    Break
}
