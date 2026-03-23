namespace Pop.Language;

public abstract class Symbol
{
    protected Symbol(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public sealed class VariableSymbol : Symbol
{
    public VariableSymbol(string name, TypeSymbol type, bool isParameter = false, bool isPublic = false)
        : base(name)
    {
        Type = type;
        IsParameter = isParameter;
        IsPublic = isPublic;
    }

    public TypeSymbol Type { get; }
    public bool IsParameter { get; }
    public bool IsPublic { get; }
}

public sealed class FunctionSymbol : Symbol
{
    public FunctionSymbol(string name, IReadOnlyList<VariableSymbol> parameters, TypeSymbol returnType, bool isPublic = false, bool isBuiltIn = false)
        : base(name)
    {
        Parameters = parameters;
        ReturnType = returnType;
        IsPublic = isPublic;
        IsBuiltIn = isBuiltIn;
    }

    public IReadOnlyList<VariableSymbol> Parameters { get; }
    public TypeSymbol ReturnType { get; private set; }
    public bool IsPublic { get; }
    public bool IsBuiltIn { get; }
    public FunctionTypeSymbol Type => new([.. Parameters.Select(static parameter => parameter.Type)], ReturnType);

    internal void SetReturnType(TypeSymbol returnType)
    {
        ReturnType = returnType;
    }
}
