namespace Pop.Language;

public abstract class TypeSymbol
{
    protected TypeSymbol(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static PrimitiveTypeSymbol Error { get; } = new("error");
    public static PrimitiveTypeSymbol Void { get; } = new("void");
    public static PrimitiveTypeSymbol Any { get; } = new("any");
    public static PrimitiveTypeSymbol Int { get; } = new("int");
    public static PrimitiveTypeSymbol Double { get; } = new("double");
    public static PrimitiveTypeSymbol Bool { get; } = new("bool");
    public static PrimitiveTypeSymbol Char { get; } = new("char");
    public static PrimitiveTypeSymbol String { get; } = new("string");

    public override string ToString() => Name;
}

public sealed class PrimitiveTypeSymbol(string name) : TypeSymbol(name);

public sealed class ArrayTypeSymbol : TypeSymbol, IEquatable<ArrayTypeSymbol>
{
    public ArrayTypeSymbol(TypeSymbol elementType)
        : base($"[{elementType.Name}]")
    {
        ElementType = elementType;
    }

    public TypeSymbol ElementType { get; }

    public bool Equals(ArrayTypeSymbol? other)
    {
        return other is not null && ElementType.Equals(other.ElementType);
    }

    public override bool Equals(object? obj) => obj is ArrayTypeSymbol other && Equals(other);

    public override int GetHashCode() => ElementType.GetHashCode();
}

public sealed class ObjectTypeSymbol : TypeSymbol, IEquatable<ObjectTypeSymbol>
{
    private readonly IReadOnlyDictionary<string, TypeSymbol> _properties;

    public ObjectTypeSymbol(IReadOnlyDictionary<string, TypeSymbol> properties)
        : base("object")
    {
        _properties = properties;
    }

    public IReadOnlyDictionary<string, TypeSymbol> Properties => _properties;

    public bool TryGetProperty(string name, out TypeSymbol type)
    {
        return _properties.TryGetValue(name, out type!);
    }

    public bool Equals(ObjectTypeSymbol? other)
    {
        if (other is null || _properties.Count != other._properties.Count)
        {
            return false;
        }

        foreach (var property in _properties)
        {
            if (!other._properties.TryGetValue(property.Key, out var otherType) || !property.Value.Equals(otherType))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is ObjectTypeSymbol other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var property in _properties.OrderBy(static property => property.Key))
        {
            hash.Add(property.Key);
            hash.Add(property.Value);
        }

        return hash.ToHashCode();
    }
}

public sealed class FunctionTypeSymbol : TypeSymbol, IEquatable<FunctionTypeSymbol>
{
    public FunctionTypeSymbol(IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
        : base("function")
    {
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }

    public IReadOnlyList<TypeSymbol> ParameterTypes { get; }
    public TypeSymbol ReturnType { get; }

    public bool Equals(FunctionTypeSymbol? other)
    {
        if (other is null ||
            ParameterTypes.Count != other.ParameterTypes.Count ||
            !ReturnType.Equals(other.ReturnType))
        {
            return false;
        }

        for (var i = 0; i < ParameterTypes.Count; i++)
        {
            if (!ParameterTypes[i].Equals(other.ParameterTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is FunctionTypeSymbol other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var parameterType in ParameterTypes)
        {
            hash.Add(parameterType);
        }

        hash.Add(ReturnType);
        return hash.ToHashCode();
    }
}
