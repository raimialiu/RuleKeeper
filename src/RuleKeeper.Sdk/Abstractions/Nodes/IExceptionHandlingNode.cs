namespace RuleKeeper.Sdk.Abstractions.Nodes;

/// <summary>
/// Represents a try statement node.
/// </summary>
public interface ITryNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the try block.
    /// </summary>
    IUnifiedSyntaxNode TryBlock { get; }

    /// <summary>
    /// Gets the catch clauses.
    /// </summary>
    IEnumerable<ICatchNode> CatchClauses { get; }

    /// <summary>
    /// Gets the finally block, if present.
    /// </summary>
    IUnifiedSyntaxNode? FinallyBlock { get; }

    /// <summary>
    /// Gets whether this try statement has a finally block.
    /// </summary>
    bool HasFinally => FinallyBlock != null;

    /// <summary>
    /// Gets whether this try statement has any catch clauses.
    /// </summary>
    bool HasCatch => CatchClauses.Any();
}

/// <summary>
/// Represents a catch clause node.
/// </summary>
public interface ICatchNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the exception type name being caught, if specified.
    /// </summary>
    string? ExceptionTypeName { get; }

    /// <summary>
    /// Gets the exception variable name, if specified.
    /// </summary>
    string? ExceptionVariableName { get; }

    /// <summary>
    /// Gets the catch block.
    /// </summary>
    IUnifiedSyntaxNode Block { get; }

    /// <summary>
    /// Gets the filter expression (when clause in C#), if present.
    /// </summary>
    IUnifiedSyntaxNode? Filter { get; }

    /// <summary>
    /// Gets whether this is a catch-all clause (catches all exceptions).
    /// </summary>
    bool IsCatchAll => string.IsNullOrEmpty(ExceptionTypeName);

    /// <summary>
    /// Gets whether this catch has a filter.
    /// </summary>
    bool HasFilter => Filter != null;

    /// <summary>
    /// Gets whether the catch block is empty.
    /// </summary>
    bool IsEmpty { get; }
}

/// <summary>
/// Represents a throw statement or expression.
/// </summary>
public interface IThrowNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the exception expression being thrown, if any.
    /// </summary>
    IUnifiedSyntaxNode? Exception { get; }

    /// <summary>
    /// Gets whether this is a rethrow (throw; without expression).
    /// </summary>
    bool IsRethrow => Exception == null;

    /// <summary>
    /// Gets whether this is a throw expression (C# 7+).
    /// </summary>
    bool IsExpression { get; }
}
