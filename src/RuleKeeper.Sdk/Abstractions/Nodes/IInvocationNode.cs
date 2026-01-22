namespace RuleKeeper.Sdk.Abstractions.Nodes;

/// <summary>
/// Represents a method or function invocation node.
/// </summary>
public interface IInvocationNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the method being invoked.
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// Gets the full method name including the target expression (e.g., "object.Method").
    /// </summary>
    string? FullMethodName { get; }

    /// <summary>
    /// Gets the target expression of the invocation (e.g., "object" in "object.Method()").
    /// </summary>
    IUnifiedSyntaxNode? Target { get; }

    /// <summary>
    /// Gets the arguments passed to the method.
    /// </summary>
    IEnumerable<IArgumentNode> Arguments { get; }

    /// <summary>
    /// Gets the number of arguments.
    /// </summary>
    int ArgumentCount => Arguments.Count();

    /// <summary>
    /// Gets the type arguments for generic method invocations.
    /// </summary>
    IEnumerable<string> TypeArguments { get; }

    /// <summary>
    /// Gets whether this is a generic method invocation.
    /// </summary>
    bool IsGeneric => TypeArguments.Any();

    /// <summary>
    /// Gets whether this is a member access invocation (a.Method()).
    /// </summary>
    bool IsMemberAccess { get; }

    /// <summary>
    /// Gets whether this is a null-conditional invocation (a?.Method()).
    /// </summary>
    bool IsNullConditional { get; }

    /// <summary>
    /// Gets whether this is an await expression.
    /// </summary>
    bool IsAwaited { get; }
}

/// <summary>
/// Represents an argument passed to a method or function.
/// </summary>
public interface IArgumentNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the argument, if it's a named argument.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the expression representing the argument value.
    /// </summary>
    IUnifiedSyntaxNode Expression { get; }

    /// <summary>
    /// Gets whether this is a named argument.
    /// </summary>
    bool IsNamed => Name != null;

    /// <summary>
    /// Gets the index of this argument in the argument list.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets whether this is a ref argument.
    /// </summary>
    bool IsRef { get; }

    /// <summary>
    /// Gets whether this is an out argument.
    /// </summary>
    bool IsOut { get; }
}
