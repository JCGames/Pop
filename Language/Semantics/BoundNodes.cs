namespace Pop.Language;

public enum BoundNodeKind
{
    CompilationUnit,
    BlockStatement,
    ExpressionStatement,
    WhileStatement,
    IfStatement,
    ReturnStatement,
    ContinueStatement,
    BreakStatement,
    FunctionDeclarationStatement,
    ErrorExpression,
    LiteralExpression,
    InjectExpression,
    NameExpression,
    VariableDeclarationExpression,
    AssignmentExpression,
    UnaryExpression,
    BinaryExpression,
    ConditionalExpression,
    CallExpression,
    MemberAccessExpression,
    ObjectExpression,
    ObjectProperty,
    ArrayExpression,
    LambdaExpression
}

public abstract class BoundNode
{
    public abstract BoundNodeKind Kind { get; }
}

public abstract class BoundStatement : BoundNode;

public abstract class BoundExpression : BoundNode
{
    protected BoundExpression(TypeSymbol type)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }
}

public sealed class BoundCompilationUnit(IReadOnlyList<BoundStatement> statements) : BoundNode
{
    public IReadOnlyList<BoundStatement> Statements { get; } = statements;
    public override BoundNodeKind Kind => BoundNodeKind.CompilationUnit;
}

public sealed class BoundBlockStatement(IReadOnlyList<BoundStatement> statements) : BoundStatement
{
    public IReadOnlyList<BoundStatement> Statements { get; } = statements;
    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
}

public sealed class BoundExpressionStatement(BoundExpression expression) : BoundStatement
{
    public BoundExpression Expression { get; } = expression;
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;
}

public sealed class BoundWhileStatement(BoundExpression condition, BoundBlockStatement body) : BoundStatement
{
    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement Body { get; } = body;
    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
}

public sealed class BoundIfStatement(
    BoundExpression condition,
    BoundBlockStatement thenStatement,
    BoundStatement? elseStatement) : BoundStatement
{
    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement ThenStatement { get; } = thenStatement;
    public BoundStatement? ElseStatement { get; } = elseStatement;
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
}

public sealed class BoundReturnStatement(BoundExpression? expression) : BoundStatement
{
    public BoundExpression? Expression { get; } = expression;
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
}

public sealed class BoundContinueStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ContinueStatement;
}

public sealed class BoundBreakStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.BreakStatement;
}

public sealed class BoundFunctionDeclarationStatement(FunctionSymbol symbol, BoundBlockStatement body) : BoundStatement
{
    public FunctionSymbol Symbol { get; } = symbol;
    public BoundBlockStatement Body { get; } = body;
    public override BoundNodeKind Kind => BoundNodeKind.FunctionDeclarationStatement;
}

public sealed class BoundErrorExpression() : BoundExpression(TypeSymbol.Error)
{
    public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
}

public sealed class BoundLiteralExpression(object? value, TypeSymbol type) : BoundExpression(type)
{
    public object? Value { get; } = value;
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
}

public sealed class BoundInjectExpression(string path, TypeSymbol type) : BoundExpression(type)
{
    public string Path { get; } = path;
    public override BoundNodeKind Kind => BoundNodeKind.InjectExpression;
}

public sealed class BoundNameExpression(Symbol symbol, TypeSymbol type) : BoundExpression(type)
{
    public Symbol Symbol { get; } = symbol;
    public override BoundNodeKind Kind => BoundNodeKind.NameExpression;
}

public sealed class BoundVariableDeclarationExpression(VariableSymbol variable, BoundExpression initializer) : BoundExpression(variable.Type)
{
    public VariableSymbol Variable { get; } = variable;
    public BoundExpression Initializer { get; } = initializer;
    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationExpression;
}

public sealed class BoundAssignmentExpression(VariableSymbol variable, BoundExpression expression) : BoundExpression(variable.Type)
{
    public VariableSymbol Variable { get; } = variable;
    public BoundExpression Expression { get; } = expression;
    public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
}

public sealed class BoundUnaryExpression(
    SyntaxKind operatorKind,
    BoundExpression operand,
    TypeSymbol type) : BoundExpression(type)
{
    public SyntaxKind OperatorKind { get; } = operatorKind;
    public BoundExpression Operand { get; } = operand;
    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
}

public sealed class BoundBinaryExpression(
    BoundExpression left,
    SyntaxKind operatorKind,
    BoundExpression right,
    TypeSymbol type) : BoundExpression(type)
{
    public BoundExpression Left { get; } = left;
    public SyntaxKind OperatorKind { get; } = operatorKind;
    public BoundExpression Right { get; } = right;
    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
}

public sealed class BoundConditionalExpression(
    BoundExpression condition,
    BoundExpression whenTrue,
    BoundExpression whenFalse,
    TypeSymbol type) : BoundExpression(type)
{
    public BoundExpression Condition { get; } = condition;
    public BoundExpression WhenTrue { get; } = whenTrue;
    public BoundExpression WhenFalse { get; } = whenFalse;
    public override BoundNodeKind Kind => BoundNodeKind.ConditionalExpression;
}

public sealed class BoundCallExpression(
    BoundExpression target,
    IReadOnlyList<BoundExpression> arguments,
    TypeSymbol type) : BoundExpression(type)
{
    public BoundExpression Target { get; } = target;
    public IReadOnlyList<BoundExpression> Arguments { get; } = arguments;
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
}

public sealed class BoundMemberAccessExpression(
    BoundExpression target,
    string memberName,
    TypeSymbol type) : BoundExpression(type)
{
    public BoundExpression Target { get; } = target;
    public string MemberName { get; } = memberName;
    public override BoundNodeKind Kind => BoundNodeKind.MemberAccessExpression;
}

public sealed class BoundObjectProperty(string name, BoundExpression value) : BoundNode
{
    public string Name { get; } = name;
    public BoundExpression Value { get; } = value;
    public override BoundNodeKind Kind => BoundNodeKind.ObjectProperty;
}

public sealed class BoundObjectExpression(IReadOnlyList<BoundObjectProperty> properties, ObjectTypeSymbol type) : BoundExpression(type)
{
    public IReadOnlyList<BoundObjectProperty> Properties { get; } = properties;
    public override BoundNodeKind Kind => BoundNodeKind.ObjectExpression;
}

public sealed class BoundArrayExpression(IReadOnlyList<BoundExpression> elements, ArrayTypeSymbol type) : BoundExpression(type)
{
    public IReadOnlyList<BoundExpression> Elements { get; } = elements;
    public override BoundNodeKind Kind => BoundNodeKind.ArrayExpression;
}

public sealed class BoundLambdaExpression(
    IReadOnlyList<VariableSymbol> parameters,
    BoundBlockStatement body,
    FunctionTypeSymbol type) : BoundExpression(type)
{
    public IReadOnlyList<VariableSymbol> Parameters { get; } = parameters;
    public BoundBlockStatement Body { get; } = body;
    public override BoundNodeKind Kind => BoundNodeKind.LambdaExpression;
}
