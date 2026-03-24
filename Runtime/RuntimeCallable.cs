using Pop.Language;

namespace Pop.Runtime;

internal interface IRuntimeCallable
{
    object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments);
}

internal sealed class BuiltInCallable(Func<EvaluationContext, IReadOnlyList<object?>, object?> implementation) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        return implementation(context, arguments);
    }
}

internal sealed class UserFunctionCallable(
    FunctionSymbol symbol,
    BoundBlockStatement body,
    RuntimeEnvironment closure,
    SourceFile sourceFile) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var local = new RuntimeEnvironment(closure);
        for (var i = 0; i < symbol.Parameters.Count; i++)
        {
            local.DeclareVariable(symbol.Parameters[i], arguments[i]);
        }

        var signal = context.ExecuteBlock(body, local, sourceFile);
        return signal.Kind == ExecutionSignalKind.Return ? signal.Value : null;
    }
}

internal sealed class LambdaCallable(
    IReadOnlyList<VariableSymbol> parameters,
    BoundBlockStatement body,
    RuntimeEnvironment closure,
    SourceFile sourceFile) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var local = new RuntimeEnvironment(closure);
        for (var i = 0; i < parameters.Count; i++)
        {
            local.DeclareVariable(parameters[i], arguments[i]);
        }

        var signal = context.ExecuteBlock(body, local, sourceFile);
        return signal.Kind == ExecutionSignalKind.Return ? signal.Value : null;
    }
}

internal sealed class ArrayAtCallable(List<object?> array) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0 || index >= array.Count)
        {
            return null;
        }

        return array[index];
    }
}

internal sealed class ArrayAddCallable(List<object?> array) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        array.Add(arguments[0]);
        return null;
    }
}

internal sealed class ArrayInsertCallable(List<object?> array) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0)
        {
            index = 0;
        }
        else if (index > array.Count)
        {
            index = array.Count;
        }

        array.Insert(index, arguments[1]);
        return null;
    }
}

internal sealed class ArrayReplaceCallable(List<object?> array) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0 || index >= array.Count)
        {
            return null;
        }

        var previous = array[index];
        array[index] = arguments[1];
        return previous;
    }
}

internal sealed class ArrayRemoveCallable(List<object?> array) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0 || index >= array.Count)
        {
            return null;
        }

        var value = array[index];
        array.RemoveAt(index);
        return value;
    }
}

internal sealed class ArrayForEachCallable(List<object?> array) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        if (arguments[0] is not IRuntimeCallable callable)
        {
            return RuntimeError.Create("for_each_failed", "forEach expects a callable argument.");
        }

        foreach (var item in array)
        {
            callable.Invoke(context, [item]);
        }

        return null;
    }
}

internal sealed class StringForEachCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        if (arguments[0] is not IRuntimeCallable callable)
        {
            return RuntimeError.Create("for_each_failed", "forEach expects a callable argument.");
        }

        foreach (var character in text)
        {
            callable.Invoke(context, [character]);
        }

        return null;
    }
}

internal sealed class StringAddCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        return text + RuntimeValueFormatter.Format(arguments[0]);
    }
}

internal sealed class StringContainsCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        return text.Contains(RuntimeValueFormatter.Format(arguments[0]), StringComparison.Ordinal);
    }
}

internal sealed class StringInsertCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0)
        {
            index = 0;
        }
        else if (index > text.Length)
        {
            index = text.Length;
        }

        return text.Insert(index, RuntimeValueFormatter.Format(arguments[1]));
    }
}

internal sealed class StringReplaceCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0 || index >= text.Length)
        {
            return text;
        }

        return text.Remove(index, 1).Insert(index, RuntimeValueFormatter.Format(arguments[1]));
    }
}

internal sealed class StringAtCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0 || index >= text.Length)
        {
            return null;
        }

        return text[index];
    }
}

internal sealed class StringRemoveCallable(string text) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var index = Convert.ToInt32(arguments[0]);
        if (index < 0 || index >= text.Length)
        {
            return text;
        }

        return text.Remove(index, 1);
    }
}

internal sealed class ObjectGetCallable(IDictionary<string, object?> properties) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var name = Convert.ToString(arguments[0]) ?? string.Empty;
        return properties.TryGetValue(name, out var value) ? value : null;
    }
}

internal sealed class ObjectAddCallable(IDictionary<string, object?> properties) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var name = Convert.ToString(arguments[0]) ?? string.Empty;
        properties[name] = arguments[1];
        return null;
    }
}

internal sealed class ObjectRemoveCallable(IDictionary<string, object?> properties) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        var name = Convert.ToString(arguments[0]) ?? string.Empty;
        if (!properties.TryGetValue(name, out var value))
        {
            return null;
        }

        properties.Remove(name);
        return value;
    }
}

internal sealed class ObjectForEachCallable(IDictionary<string, object?> properties) : IRuntimeCallable
{
    public object? Invoke(EvaluationContext context, IReadOnlyList<object?> arguments)
    {
        var propagatedError = RuntimeError.PropagateFirst(arguments);
        if (propagatedError is not null)
        {
            return propagatedError;
        }

        if (arguments[0] is not IRuntimeCallable callable)
        {
            return RuntimeError.Create("for_each_failed", "forEach expects a callable argument.");
        }

        foreach (var pair in properties)
        {
            callable.Invoke(context, [pair.Key, pair.Value]);
        }

        return null;
    }
}
