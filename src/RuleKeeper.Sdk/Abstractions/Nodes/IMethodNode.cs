namespace RuleKeeper.Sdk.Abstractions.Nodes;

/// <summary>
/// Represents a method or function declaration node with specialized properties.
/// </summary>
public interface IMethodNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the parameters of the method.
    /// </summary>
    IEnumerable<IParameterNode> Parameters { get; }

    /// <summary>
    /// Gets the number of parameters.
    /// </summary>
    int ParameterCount => Parameters.Count();

    /// <summary>
    /// Gets the return type name, if specified.
    /// </summary>
    string? ReturnTypeName { get; }

    /// <summary>
    /// Gets the body of the method, if it has one.
    /// </summary>
    IUnifiedSyntaxNode? Body { get; }

    /// <summary>
    /// Gets whether this is an async method.
    /// </summary>
    bool IsAsync { get; }

    /// <summary>
    /// Gets whether this is a static method.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Gets whether this is a public method.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets whether this is a private method.
    /// </summary>
    bool IsPrivate { get; }

    /// <summary>
    /// Gets whether this is a protected method.
    /// </summary>
    bool IsProtected { get; }

    /// <summary>
    /// Gets whether this is an abstract method.
    /// </summary>
    bool IsAbstract { get; }

    /// <summary>
    /// Gets whether this is a virtual method.
    /// </summary>
    bool IsVirtual { get; }

    /// <summary>
    /// Gets whether this is an override method.
    /// </summary>
    bool IsOverride { get; }

    /// <summary>
    /// Gets whether this method has a body (not abstract or interface member).
    /// </summary>
    bool HasBody { get; }

    /// <summary>
    /// Gets the modifiers as a collection of strings.
    /// </summary>
    IEnumerable<string> Modifiers { get; }

    /// <summary>
    /// Gets the type parameters for generic methods.
    /// </summary>
    IEnumerable<string> TypeParameters { get; }

    /// <summary>
    /// Gets whether this is a generic method.
    /// </summary>
    bool IsGeneric => TypeParameters.Any();
}

/// <summary>
/// Represents a parameter declaration node.
/// </summary>
public interface IParameterNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type name of the parameter.
    /// </summary>
    string? TypeName { get; }

    /// <summary>
    /// Gets whether this parameter has a default value.
    /// </summary>
    bool HasDefaultValue { get; }

    /// <summary>
    /// Gets the default value expression, if any.
    /// </summary>
    IUnifiedSyntaxNode? DefaultValue { get; }

    /// <summary>
    /// Gets whether this is a variadic/params parameter.
    /// </summary>
    bool IsParams { get; }

    /// <summary>
    /// Gets whether this is a reference parameter (ref/out in C#).
    /// </summary>
    bool IsRef { get; }

    /// <summary>
    /// Gets whether this is an output parameter (out in C#).
    /// </summary>
    bool IsOut { get; }

    /// <summary>
    /// Gets the index of this parameter in the parameter list.
    /// </summary>
    int Index { get; }
}
