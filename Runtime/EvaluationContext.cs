using Pop.Language;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Pop.Runtime;

internal sealed class EvaluationContext
{
    private static readonly HttpClient HttpClient = new();
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
            BoundMemberAssignmentExpression memberAssignment => EvaluateMemberAssignmentExpression(memberAssignment, environment, sourceFile),
            BoundUnaryExpression unary => EvaluateUnaryExpression(unary, environment, sourceFile),
            BoundPostfixUpdateExpression postfixUpdate => EvaluateExpression(postfixUpdate.Expression, environment, sourceFile),
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
        SemanticModel model;
        try
        {
            model = ModuleLoader.LoadSemanticModel(resolvedPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return RuntimeError.Create("inject_failed", exception.Message);
        }

        if (model.Diagnostics.Count > 0)
        {
            return RuntimeError.Create("inject_failed", string.Join(Environment.NewLine, model.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        var injectedFile = model.ParseResult.SourceFile;
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
        var fs = new Dictionary<string, object?>(StringComparer.Ordinal);
        var math = new Dictionary<string, object?>(StringComparer.Ordinal);
        var json = new Dictionary<string, object?>(StringComparer.Ordinal);
        var http = new Dictionary<string, object?>(StringComparer.Ordinal);

        RegisterBuiltIn(corn, BuiltInSymbols.Print, new BuiltInCallable((_, arguments) =>
        {
            _output.Write(RuntimeValueFormatter.Format(arguments[0]));
            return null;
        }), propagateErrors: false);

        RegisterBuiltIn(corn, BuiltInSymbols.PrintLn, new BuiltInCallable((_, arguments) =>
        {
            _output.WriteLine(RuntimeValueFormatter.Format(arguments[0]));
            return null;
        }), propagateErrors: false);

        RegisterBuiltIn(corn, BuiltInSymbols.Type, new BuiltInCallable((_, arguments) => GetTypeName(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.Str, new BuiltInCallable((_, arguments) => RuntimeValueFormatter.Format(arguments[0])), propagateErrors: false);
        RegisterBuiltIn(corn, BuiltInSymbols.Int, new BuiltInCallable((_, arguments) => ConvertToInt(arguments[0], "int_conversion_failed", "int conversion failed.")));
        RegisterBuiltIn(corn, BuiltInSymbols.Double, new BuiltInCallable((_, arguments) => ConvertToDouble(arguments[0], "double_conversion_failed", "double conversion failed.")));
        RegisterBuiltIn(corn, BuiltInSymbols.Bool, new BuiltInCallable((_, arguments) => ConvertToBool(arguments[0])));
        RegisterBuiltIn(corn, BuiltInSymbols.IsError, new BuiltInCallable((_, arguments) => RuntimeError.IsError(arguments[0])), propagateErrors: false);
        RegisterBuiltIn(corn, BuiltInSymbols.Error, new BuiltInCallable((_, arguments) =>
            RuntimeError.Create(
                Convert.ToString(arguments[0]) ?? string.Empty,
                Convert.ToString(arguments[1]) ?? string.Empty)), propagateErrors: false);
        RegisterBuiltIn(corn, BuiltInSymbols.Input, new BuiltInCallable((_, _) => _input.ReadLine() ?? string.Empty), propagateErrors: false);

        RegisterBuiltIn(corn, BuiltInSymbols.Keys, new BuiltInCallable((_, arguments) => arguments[0] switch
        {
            IReadOnlyDictionary<string, object?> properties => properties.Keys.Cast<object?>().ToList(),
            _ => RuntimeError.Create("keys_failed", "keys expects an object.")
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Has, new BuiltInCallable((_, arguments) => arguments[0] switch
        {
            IReadOnlyDictionary<string, object?> properties => properties.ContainsKey(Convert.ToString(arguments[1]) ?? string.Empty),
            _ => RuntimeError.Create("has_failed", "has expects an object as its first argument.")
        }));

        RegisterBuiltIn(corn, BuiltInSymbols.Clock, new BuiltInCallable((_, _) =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d));

        RegisterBuiltIn(fs, BuiltInSymbols.FsRead, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return File.ReadAllText(path);
        }), errorCode: "file_read_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsWrite, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            var text = Convert.ToString(arguments[1]) ?? string.Empty;
            File.WriteAllText(path, text);
            return null;
        }), errorCode: "file_write_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsAppend, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            var text = Convert.ToString(arguments[1]) ?? string.Empty;
            File.AppendAllText(path, text);
            return null;
        }), errorCode: "file_append_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsCopy, new BuiltInCallable((_, arguments) =>
        {
            var source = ResolvePath(arguments[0]);
            var destination = ResolvePath(arguments[1]);
            File.Copy(source, destination, overwrite: true);
            return null;
        }), errorCode: "file_copy_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsMove, new BuiltInCallable((_, arguments) =>
        {
            var source = ResolvePath(arguments[0]);
            var destination = ResolvePath(arguments[1]);
            MoveFileSystemEntry(source, destination);
            return null;
        }), errorCode: "file_move_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsRemove, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            RemoveFileSystemEntry(path);
            return null;
        }), errorCode: "file_remove_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsExists, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return File.Exists(path) || Directory.Exists(path);
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsIsFile, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return File.Exists(path);
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsIsDir, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return Directory.Exists(path);
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsInfo, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return CreateFileInfoObject(path);
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsSize, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            if (!File.Exists(path))
            {
                return RuntimeError.Create("file_size_failed", $"File '{path}' was not found.");
            }

            return new FileInfo(path).Length;
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsModified, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return GetFileSystemTimestamp(path, timestampKind: "modified");
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsCreated, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return GetFileSystemTimestamp(path, timestampKind: "created");
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsList, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return ListDirectoryEntries(path, ListKind.All);
        }));

        RegisterBuiltIn(fs, BuiltInSymbols.FsCwd, new BuiltInCallable((_, _) => Directory.GetCurrentDirectory()), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsFiles, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return ListDirectoryEntries(path, ListKind.Files);
        }));

        RegisterBuiltIn(fs, BuiltInSymbols.FsDirs, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return ListDirectoryEntries(path, ListKind.Directories);
        }));

        RegisterBuiltIn(fs, BuiltInSymbols.FsMkdir, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            Directory.CreateDirectory(path);
            return null;
        }), errorCode: "file_mkdir_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsChdir, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            Directory.SetCurrentDirectory(path);
            return null;
        }), errorCode: "file_chdir_failed");

        RegisterBuiltIn(fs, BuiltInSymbols.FsJoin, new BuiltInCallable((_, arguments) =>
        {
            var left = Convert.ToString(arguments[0]) ?? string.Empty;
            var right = Convert.ToString(arguments[1]) ?? string.Empty;
            return Path.Combine(left, right);
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsName, new BuiltInCallable((_, arguments) =>
        {
            var path = Convert.ToString(arguments[0]) ?? string.Empty;
            return Path.GetFileName(NormalizePathForInspection(path));
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsStem, new BuiltInCallable((_, arguments) =>
        {
            var path = Convert.ToString(arguments[0]) ?? string.Empty;
            return Path.GetFileNameWithoutExtension(NormalizePathForInspection(path));
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsExt, new BuiltInCallable((_, arguments) =>
        {
            var path = Convert.ToString(arguments[0]) ?? string.Empty;
            return Path.GetExtension(NormalizePathForInspection(path));
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsParent, new BuiltInCallable((_, arguments) =>
        {
            var path = ResolvePath(arguments[0]);
            return Path.GetDirectoryName(path);
        }), propagateErrors: false);

        RegisterBuiltIn(fs, BuiltInSymbols.FsAbsolute, new BuiltInCallable((_, arguments) => ResolvePath(arguments[0])), propagateErrors: false);

        math["pi"] = Math.PI;
        math["tau"] = Math.Tau;
        math["e"] = Math.E;

        RegisterBuiltIn(math, BuiltInSymbols.MathAbs, new BuiltInCallable((_, arguments) => EvaluateMathAbs(arguments[0])));
        RegisterBuiltIn(math, BuiltInSymbols.MathMin, new BuiltInCallable((_, arguments) => EvaluateMathMin(arguments[0], arguments[1])));
        RegisterBuiltIn(math, BuiltInSymbols.MathMax, new BuiltInCallable((_, arguments) => EvaluateMathMax(arguments[0], arguments[1])));
        RegisterBuiltIn(math, BuiltInSymbols.MathClamp, new BuiltInCallable((_, arguments) => EvaluateMathClamp(arguments[0], arguments[1], arguments[2])));
        RegisterBuiltIn(math, BuiltInSymbols.MathSqrt, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "sqrt_failed", "sqrt is only defined for values greater than or equal to zero.", Math.Sqrt, value => value >= 0)));
        RegisterBuiltIn(math, BuiltInSymbols.MathPow, new BuiltInCallable((_, arguments) => EvaluateMathBinary(arguments[0], arguments[1], Math.Pow)));
        RegisterBuiltIn(math, BuiltInSymbols.MathSin, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "sin_failed", "sin failed.", Math.Sin)));
        RegisterBuiltIn(math, BuiltInSymbols.MathCos, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "cos_failed", "cos failed.", Math.Cos)));
        RegisterBuiltIn(math, BuiltInSymbols.MathTan, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "tan_failed", "tan failed.", Math.Tan)));
        RegisterBuiltIn(math, BuiltInSymbols.MathAsin, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "asin_failed", "asin is only defined for values from -1 to 1.", Math.Asin, value => value >= -1 && value <= 1)));
        RegisterBuiltIn(math, BuiltInSymbols.MathAcos, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "acos_failed", "acos is only defined for values from -1 to 1.", Math.Acos, value => value >= -1 && value <= 1)));
        RegisterBuiltIn(math, BuiltInSymbols.MathAtan, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "atan_failed", "atan failed.", Math.Atan)));
        RegisterBuiltIn(math, BuiltInSymbols.MathAtan2, new BuiltInCallable((_, arguments) => EvaluateMathBinary(arguments[0], arguments[1], Math.Atan2)));
        RegisterBuiltIn(math, BuiltInSymbols.MathLog, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "log_failed", "log is only defined for values greater than zero.", Math.Log, value => value > 0)));
        RegisterBuiltIn(math, BuiltInSymbols.MathLog10, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "log10_failed", "log10 is only defined for values greater than zero.", Math.Log10, value => value > 0)));
        RegisterBuiltIn(math, BuiltInSymbols.MathLog2, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "log2_failed", "log2 is only defined for values greater than zero.", Math.Log2, value => value > 0)));
        RegisterBuiltIn(math, BuiltInSymbols.MathExp, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "exp_failed", "exp failed.", Math.Exp)));
        RegisterBuiltIn(math, BuiltInSymbols.MathFloor, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "floor_failed", "floor failed.", Math.Floor)));
        RegisterBuiltIn(math, BuiltInSymbols.MathCeil, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "ceil_failed", "ceil failed.", Math.Ceiling)));
        RegisterBuiltIn(math, BuiltInSymbols.MathRound, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "round_failed", "round failed.", Math.Round)));
        RegisterBuiltIn(math, BuiltInSymbols.MathTrunc, new BuiltInCallable((_, arguments) => EvaluateMathUnary(arguments[0], "trunc_failed", "trunc failed.", Math.Truncate)));

        RegisterBuiltIn(json, BuiltInSymbols.JsonParse, new BuiltInCallable((_, arguments) =>
        {
            var text = Convert.ToString(arguments[0]) ?? string.Empty;
            using var document = JsonDocument.Parse(text);
            return ConvertJsonElement(document.RootElement);
        }), errorCode: "json_parse_failed");

        RegisterBuiltIn(json, BuiltInSymbols.JsonStringify, new BuiltInCallable((_, arguments) => SerializeJsonValue(arguments[0], indented: false)));
        RegisterBuiltIn(json, BuiltInSymbols.JsonPretty, new BuiltInCallable((_, arguments) => SerializeJsonValue(arguments[0], indented: true)));

        RegisterBuiltIn(http, BuiltInSymbols.HttpGet, new BuiltInCallable((_, arguments) => ExecuteHttpRequest("GET", arguments[0], null, null)), errorCode: "http_request_failed");
        RegisterBuiltIn(http, BuiltInSymbols.HttpPost, new BuiltInCallable((_, arguments) => ExecuteHttpRequest("POST", arguments[0], arguments[1], null)), errorCode: "http_request_failed");
        RegisterBuiltIn(http, BuiltInSymbols.HttpPut, new BuiltInCallable((_, arguments) => ExecuteHttpRequest("PUT", arguments[0], arguments[1], null)), errorCode: "http_request_failed");
        RegisterBuiltIn(http, BuiltInSymbols.HttpDelete, new BuiltInCallable((_, arguments) => ExecuteHttpRequest("DELETE", arguments[0], null, null)), errorCode: "http_request_failed");
        RegisterBuiltIn(http, BuiltInSymbols.HttpRequest, new BuiltInCallable((_, arguments) => ExecuteHttpRequest(arguments[0], arguments[1], arguments[2], arguments[3])), errorCode: "http_request_failed");

        corn["fs"] = fs;
        corn["math"] = math;
        corn["json"] = json;
        corn["http"] = http;

        environment.DeclareVariable(BuiltInVariables.Corn, corn);
    }

    private static void RegisterBuiltIn(
        IDictionary<string, object?> corn,
        FunctionSymbol symbol,
        IRuntimeCallable callable,
        bool propagateErrors = true,
        string? errorCode = null)
    {
        corn[symbol.Name] = new SafeBuiltInCallable(callable, symbol.Name, propagateErrors, errorCode);
    }

    private object? EvaluateNameExpression(BoundNameExpression name, RuntimeEnvironment environment)
    {
        return name.Symbol switch
        {
            VariableSymbol variable when environment.TryGetVariable(variable, out var value) => value,
            FunctionSymbol function when environment.TryGetFunction(function, out var callable) => callable,
            _ => RuntimeError.Create("undefined_runtime_symbol", $"Undefined runtime symbol '{name.Symbol.Name}'.")
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
            return RuntimeError.Create("undefined_runtime_variable", $"Undefined runtime variable '{assignment.Variable.Name}'.");
        }

        return value;
    }

    private object? EvaluateMemberAssignmentExpression(BoundMemberAssignmentExpression assignment, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var target = EvaluateExpression(assignment.Target, environment, sourceFile);
        var propagatedError = RuntimeError.Propagate(target);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        if (target is not IDictionary<string, object?> properties)
        {
            return RuntimeError.Create("invalid_member_assignment_target", "Member assignment target is not an object.");
        }

        var value = EvaluateExpression(assignment.Expression, environment, sourceFile);
        properties[assignment.MemberName] = value;
        return value;
    }

    private object? EvaluateUnaryExpression(BoundUnaryExpression unary, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var operand = EvaluateExpression(unary.Operand, environment, sourceFile);
        if (RuntimeError.IsError(operand))
        {
            return operand;
        }

        return unary.OperatorKind switch
        {
            SyntaxKind.PlusToken => operand,
            SyntaxKind.MinusToken when operand is long integer => -integer,
            SyntaxKind.MinusToken when operand is double floating => -floating,
            SyntaxKind.BangToken when operand is bool boolean => !boolean,
            SyntaxKind.TildeToken when operand is long integer => ~integer,
            _ => RuntimeError.Create("invalid_unary_operation", $"Unsupported unary operation '{unary.OperatorKind}'.")
        };
    }

    private object? EvaluateBinaryExpression(BoundBinaryExpression binary, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var left = EvaluateExpression(binary.Left, environment, sourceFile);
        var right = EvaluateExpression(binary.Right, environment, sourceFile);
        var propagatedError = RuntimeError.Propagate(left, right);
        if (propagatedError is not null)
        {
            return propagatedError!;
        }

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
            _ => RuntimeError.Create("invalid_binary_operation", $"Unsupported binary operation '{binary.OperatorKind}'.")
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
        if (RuntimeError.IsError(target))
        {
            return target;
        }

        if (target is not IRuntimeCallable callable)
        {
            return RuntimeError.Create("invalid_call_target", "Call target is not callable.");
        }

        var arguments = call.Arguments.Select(argument => EvaluateExpression(argument, environment, sourceFile)).ToArray();
        return callable.Invoke(this, arguments);
    }

    private object? EvaluateMemberAccessExpression(BoundMemberAccessExpression memberAccess, RuntimeEnvironment environment, SourceFile sourceFile)
    {
        var target = EvaluateExpression(memberAccess.Target, environment, sourceFile);
        if (target is long integer)
        {
            return memberAccess.MemberName switch
            {
                "min" => long.MinValue,
                "max" => long.MaxValue,
                _ => RuntimeError.Create("invalid_member_access", $"Int does not contain member '{memberAccess.MemberName}'.")
            };
        }

        if (target is double floating)
        {
            return memberAccess.MemberName switch
            {
                "min" => double.MinValue,
                "max" => double.MaxValue,
                _ => RuntimeError.Create("invalid_member_access", $"Double does not contain member '{memberAccess.MemberName}'.")
            };
        }

        if (target is string text)
        {
            return memberAccess.MemberName switch
            {
                "len" => (long)text.Length,
                "at" => new StringAtCallable(text),
                "add" => new StringAddCallable(text),
                "contains" => new StringContainsCallable(text),
                "insert" => new StringInsertCallable(text),
                "replace" => new StringReplaceCallable(text),
                "remove" => new StringRemoveCallable(text),
                "forEach" => new StringForEachCallable(text),
                _ => RuntimeError.Create("invalid_member_access", $"String does not contain member '{memberAccess.MemberName}'.")
            };
        }

        if (target is List<object?> array)
        {
            return memberAccess.MemberName switch
            {
                "len" => (long)array.Count,
                "at" => new ArrayAtCallable(array),
                "add" => new ArrayAddCallable(array),
                "insert" => new ArrayInsertCallable(array),
                "replace" => new ArrayReplaceCallable(array),
                "remove" => new ArrayRemoveCallable(array),
                "forEach" => new ArrayForEachCallable(array),
                _ => RuntimeError.Create("invalid_member_access", $"Array does not contain member '{memberAccess.MemberName}'.")
            };
        }

        if (target is IReadOnlyDictionary<string, object?> properties &&
            properties.TryGetValue(memberAccess.MemberName, out var value))
        {
            return value;
        }

        if (target is IDictionary<string, object?> mutableObjectProperties)
        {
            return memberAccess.MemberName switch
            {
                "len" => (long)mutableObjectProperties.Count,
                "get" => new ObjectGetCallable(mutableObjectProperties),
                "add" => new ObjectAddCallable(mutableObjectProperties),
                "remove" => new ObjectRemoveCallable(mutableObjectProperties),
                "forEach" => new ObjectForEachCallable(mutableObjectProperties),
                _ => RuntimeError.Create("invalid_member_access", $"Object does not contain member '{memberAccess.MemberName}'.")
            };
        }

        return RuntimeError.Create("invalid_member_access", $"Object does not contain member '{memberAccess.MemberName}'.");
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
        try
        {
            if (left is double || right is double)
            {
                return floatingOperation(Convert.ToDouble(left), Convert.ToDouble(right));
            }

            return integerOperation(Convert.ToInt64(left), Convert.ToInt64(right));
        }
        catch
        {
            return RuntimeError.Create("numeric_operation_failed", "Numeric operation failed.");
        }
    }

    private static object EvaluateComparison(
        object? left,
        object? right,
        Func<long, long, bool> integerOperation,
        Func<double, double, bool> floatingOperation)
    {
        try
        {
            if (left is double || right is double)
            {
                return floatingOperation(Convert.ToDouble(left), Convert.ToDouble(right));
            }

            return integerOperation(Convert.ToInt64(left), Convert.ToInt64(right));
        }
        catch
        {
            return RuntimeError.Create("comparison_failed", "Comparison failed.");
        }
    }

    private static bool IsTrue(object? value)
    {
        return value is true;
    }

    private static object ConvertToInt(object? value, string errorCode, string errorMessage)
    {
        try
        {
            return value switch
            {
                null => 0L,
                long integer => integer,
                double floating => (long)floating,
                bool boolean => boolean ? 1L : 0L,
                char character => character,
                string text when long.TryParse(text, out var parsed) => parsed,
                string text when double.TryParse(text, out var floating) => (long)floating,
                _ => RuntimeError.Create(errorCode, errorMessage)
            };
        }
        catch
        {
            return RuntimeError.Create(errorCode, errorMessage);
        }
    }

    private static object ConvertToDouble(object? value, string errorCode, string errorMessage)
    {
        try
        {
            return value switch
            {
                null => 0d,
                long integer => integer,
                double floating => floating,
                bool boolean => boolean ? 1d : 0d,
                char character => character,
                string text when double.TryParse(text, out var parsed) => parsed,
                _ => RuntimeError.Create(errorCode, errorMessage)
            };
        }
        catch
        {
            return RuntimeError.Create(errorCode, errorMessage);
        }
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
            _ when RuntimeError.IsError(value) => "error",
            null => "nil",
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

    private static string NormalizePathForInspection(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrEmpty(trimmed) ? path : trimmed;
    }

    private static object GetFileSystemTimestamp(string path, string timestampKind)
    {
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            return timestampKind == "created"
                ? new DateTimeOffset(info.CreationTimeUtc).ToUnixTimeSeconds()
                : new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
        }

        if (Directory.Exists(path))
        {
            var info = new DirectoryInfo(path);
            return timestampKind == "created"
                ? new DateTimeOffset(info.CreationTimeUtc).ToUnixTimeSeconds()
                : new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
        }

        return RuntimeError.Create($"file_{timestampKind}_failed", $"Path '{path}' was not found.");
    }

    private static object ListDirectoryEntries(string path, ListKind kind)
    {
        if (!Directory.Exists(path))
        {
            return RuntimeError.Create("file_list_failed", $"Directory '{path}' was not found.");
        }

        IEnumerable<string> entries = kind switch
        {
            ListKind.Files => Directory.EnumerateFiles(path),
            ListKind.Directories => Directory.EnumerateDirectories(path),
            _ => Directory.EnumerateFileSystemEntries(path)
        };

        return entries
            .Select(static entry => (object?)Path.GetFileName(entry))
            .ToList();
    }

    private static void MoveFileSystemEntry(string source, string destination)
    {
        if (File.Exists(source))
        {
            File.Move(source, destination, overwrite: true);
            return;
        }

        if (Directory.Exists(source))
        {
            if (Directory.Exists(destination))
            {
                throw new IOException($"Directory '{destination}' already exists.");
            }

            Directory.Move(source, destination);
            return;
        }

        throw new IOException($"Path '{source}' was not found.");
    }

    private static void RemoveFileSystemEntry(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            return;
        }

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: false);
            return;
        }

        throw new IOException($"Path '{path}' was not found.");
    }

    private static IDictionary<string, object?> CreateFileInfoObject(string path)
    {
        var exists = File.Exists(path) || Directory.Exists(path);
        var isFile = File.Exists(path);
        var isDir = Directory.Exists(path);

        DateTimeOffset? created = null;
        DateTimeOffset? modified = null;
        long? size = null;

        if (isFile)
        {
            var fileInfo = new FileInfo(path);
            created = fileInfo.CreationTimeUtc;
            modified = fileInfo.LastWriteTimeUtc;
            size = fileInfo.Length;
        }
        else if (isDir)
        {
            var directoryInfo = new DirectoryInfo(path);
            created = directoryInfo.CreationTimeUtc;
            modified = directoryInfo.LastWriteTimeUtc;
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["path"] = path,
            ["name"] = Path.GetFileName(path),
            ["exists"] = exists,
            ["isFile"] = isFile,
            ["isDir"] = isDir,
            ["size"] = size is null ? null : size.Value,
            ["created"] = created is null ? null : created.Value.ToUnixTimeSeconds(),
            ["modified"] = modified is null ? null : modified.Value.ToUnixTimeSeconds()
        };
    }

    private static object EvaluateMathAbs(object? value)
    {
        if (RuntimeError.IsError(value))
        {
            return value;
        }

        return value switch
        {
            long integer => Math.Abs(integer),
            double floating => Math.Abs(floating),
            _ => RuntimeError.Create("abs_failed", "abs expects an int or double.")
        };
    }

    private static object EvaluateMathMin(object? left, object? right)
    {
        var propagatedError = RuntimeError.Propagate(left, right);
        if (propagatedError is not null)
        {
            return propagatedError!;
        }

        if (left is long leftInt && right is long rightInt)
        {
            return Math.Min(leftInt, rightInt);
        }

        if (TryConvertToDouble(left, out var leftDouble) && TryConvertToDouble(right, out var rightDouble))
        {
            return Math.Min(leftDouble, rightDouble);
        }

        return RuntimeError.Create("min_failed", "min expects numeric values.");
    }

    private static object EvaluateMathMax(object? left, object? right)
    {
        var propagatedError = RuntimeError.Propagate(left, right);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        if (left is long leftInt && right is long rightInt)
        {
            return Math.Max(leftInt, rightInt);
        }

        if (TryConvertToDouble(left, out var leftDouble) && TryConvertToDouble(right, out var rightDouble))
        {
            return Math.Max(leftDouble, rightDouble);
        }

        return RuntimeError.Create("max_failed", "max expects numeric values.");
    }

    private static object EvaluateMathClamp(object? value, object? minValue, object? maxValue)
    {
        var propagatedError = RuntimeError.Propagate(value, minValue, maxValue);
        if (propagatedError is not null)
        {
            return propagatedError!;
        }

        if (value is long integer && minValue is long minInteger && maxValue is long maxInteger)
        {
            return Math.Clamp(integer, minInteger, maxInteger);
        }

        if (TryConvertToDouble(value, out var floating) &&
            TryConvertToDouble(minValue, out var minFloating) &&
            TryConvertToDouble(maxValue, out var maxFloating))
        {
            return Math.Clamp(floating, minFloating, maxFloating);
        }

        return RuntimeError.Create("clamp_failed", "clamp expects numeric values.");
    }

    private static object EvaluateMathUnary(
        object? value,
        string errorCode,
        string errorMessage,
        Func<double, double> operation,
        Func<double, bool>? guard = null)
    {
        if (RuntimeError.IsError(value))
        {
            return value;
        }

        if (!TryConvertToDouble(value, out var number))
        {
            return RuntimeError.Create(errorCode, "Math functions expect numeric values.");
        }

        if (guard is not null && !guard(number))
        {
            return RuntimeError.Create(errorCode, errorMessage);
        }

        return operation(number);
    }

    private static object EvaluateMathBinary(
        object? left,
        object? right,
        Func<double, double, double> operation)
    {
        var propagatedError = RuntimeError.Propagate(left, right);
        if (propagatedError is not null)
        {
            return propagatedError!;
        }

        if (!TryConvertToDouble(left, out var leftNumber) || !TryConvertToDouble(right, out var rightNumber))
        {
            return RuntimeError.Create("math_failed", "Math functions expect numeric values.");
        }

        return operation(leftNumber, rightNumber);
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        switch (value)
        {
            case long integer:
                result = integer;
                return true;
            case double floating:
                result = floating;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.False => false,
            JsonValueKind.True => true,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    static property => property.Name,
                    property => ConvertJsonElement(property.Value),
                    StringComparer.Ordinal),
            _ => RuntimeError.Create("json_parse_failed", "Unsupported JSON value.")
        };
    }

    private static object? ConvertToJsonValue(object? value)
    {
        if (value is null or string or bool or long or double)
        {
            return value;
        }

        if (value is char character)
        {
            return character.ToString();
        }

        if (value is IReadOnlyDictionary<string, object?> properties)
        {
            return properties.ToDictionary(
                static property => property.Key,
                property => ConvertToJsonValue(property.Value),
                StringComparer.Ordinal);
        }

        if (value is IEnumerable<object?> values)
        {
            return values.Select(ConvertToJsonValue).ToList();
        }

        throw new InvalidOperationException("json.stringify can only serialize nil, bool, int, double, char, string, arrays, and objects.");
    }

    private static string SerializeJsonValue(object? value, bool indented)
    {
        return JsonSerializer.Serialize(
            ConvertToJsonValue(value),
            new JsonSerializerOptions
            {
                WriteIndented = indented
            });
    }

    private static object ExecuteHttpRequest(object? methodValue, object? urlValue, object? bodyValue, object? headersValue)
    {
        var methodText = Convert.ToString(methodValue) ?? string.Empty;
        var url = Convert.ToString(urlValue) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(methodText))
        {
            return RuntimeError.Create("http_request_failed", "HTTP method is required.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return RuntimeError.Create("http_request_failed", "HTTP url is required.");
        }

        var method = new HttpMethod(methodText.ToUpperInvariant());
        using var request = new HttpRequestMessage(method, url);

        if (bodyValue is not null)
        {
            request.Content = new StringContent(Convert.ToString(bodyValue) ?? string.Empty, Encoding.UTF8);
        }

        ApplyHttpHeaders(request, headersValue);

        using var response = HttpClient.Send(request);
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return CreateHttpResponseObject(method.Method, response, body);
    }

    private static void ApplyHttpHeaders(HttpRequestMessage request, object? headersValue)
    {
        if (headersValue is null)
        {
            return;
        }

        if (headersValue is not IReadOnlyDictionary<string, object?> headers)
        {
            throw new InvalidOperationException("http.request headers must be an object.");
        }

        foreach (var header in headers)
        {
            var value = Convert.ToString(header.Value) ?? string.Empty;
            if (string.Equals(header.Key, "content-type", StringComparison.OrdinalIgnoreCase))
            {
                request.Content ??= new StringContent(string.Empty, Encoding.UTF8);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(header.Key, value))
            {
                request.Content ??= new StringContent(string.Empty, Encoding.UTF8);
                request.Content.Headers.TryAddWithoutValidation(header.Key, value);
            }
        }
    }

    private static IDictionary<string, object?> CreateHttpResponseObject(string method, HttpResponseMessage response, string body)
    {
        var headers = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var header in response.Headers)
        {
            headers[header.Key.ToLowerInvariant()] = string.Join(", ", header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key.ToLowerInvariant()] = string.Join(", ", header.Value);
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ok"] = response.IsSuccessStatusCode,
            ["status"] = (long)(int)response.StatusCode,
            ["reason"] = response.ReasonPhrase ?? string.Empty,
            ["body"] = body,
            ["headers"] = headers,
            ["url"] = response.RequestMessage?.RequestUri?.ToString() ?? string.Empty,
            ["method"] = method
        };
    }
}

internal enum ListKind
{
    All,
    Files,
    Directories
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

internal sealed class SafeBuiltInCallable(
    IRuntimeCallable inner,
    string name,
    bool propagateErrors,
    string? errorCode) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        if (propagateErrors)
        {
            var propagatedError = RuntimeError.PropagateFirst(arguments);
            if (propagatedError is not null)
            {
                return propagatedError;
            }
        }

        try
        {
            return inner.Invoke(context, arguments);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or FormatException or OverflowException or ArgumentException or JsonException or HttpRequestException or TaskCanceledException)
        {
            return RuntimeError.Create(errorCode ?? $"{name}_failed", exception.Message);
        }
    }
}
