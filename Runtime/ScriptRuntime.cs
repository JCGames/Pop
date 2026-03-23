using Pop.Language;

namespace Pop.Runtime;

public sealed class ScriptRuntime
{
    private readonly TextWriter _output;
    private readonly TextReader _input;

    public ScriptRuntime(TextWriter? output = null, TextReader? input = null)
    {
        _output = output ?? TextWriter.Null;
        _input = input ?? TextReader.Null;
    }

    public RuntimeExecutionResult Execute(SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Diagnostics.Count > 0)
        {
            return new RuntimeExecutionResult(model.Diagnostics, succeeded: false);
        }

        var context = new EvaluationContext(_output, _input);
        var environment = context.CreateGlobalEnvironment();
        context.ExecuteCompilationUnit(model.Root, environment, model.ParseResult.SourceFile);
        return new RuntimeExecutionResult([], succeeded: true);
    }

    public RuntimeExecutionResult Execute(SourceFile sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
        return Execute(SemanticModel.Create(sourceFile));
    }

    public RuntimeExecutionResult ExecuteText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Execute(SemanticModel.CreateText(text));
    }
}

public sealed class RuntimeExecutionResult(IReadOnlyList<Diagnostic> diagnostics, bool succeeded)
{
    public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;
    public bool Succeeded { get; } = succeeded;
}
