using Pop.Language;

namespace Pop.Runtime;

internal sealed class RuntimeEnvironment
{
    private readonly Dictionary<VariableSymbol, object?> _variables = [];
    private readonly Dictionary<FunctionSymbol, IRuntimeCallable> _functions = [];

    public RuntimeEnvironment(RuntimeEnvironment? parent)
    {
        Parent = parent;
    }

    public RuntimeEnvironment? Parent { get; }

    public void DeclareVariable(VariableSymbol symbol, object? value)
    {
        _variables[symbol] = value;
    }

    public bool TryAssignVariable(VariableSymbol symbol, object? value)
    {
        for (var environment = this; environment is not null; environment = environment.Parent)
        {
            if (environment._variables.ContainsKey(symbol))
            {
                environment._variables[symbol] = value;
                return true;
            }
        }

        return false;
    }

    public bool TryGetVariable(VariableSymbol symbol, out object? value)
    {
        for (var environment = this; environment is not null; environment = environment.Parent)
        {
            if (environment._variables.TryGetValue(symbol, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    public void DeclareFunction(FunctionSymbol symbol, IRuntimeCallable callable)
    {
        _functions[symbol] = callable;
    }

    public bool TryGetFunction(FunctionSymbol symbol, out IRuntimeCallable callable)
    {
        for (var environment = this; environment is not null; environment = environment.Parent)
        {
            if (environment._functions.TryGetValue(symbol, out callable!))
            {
                return true;
            }
        }

        callable = null!;
        return false;
    }
}
