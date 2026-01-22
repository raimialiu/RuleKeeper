namespace RuleKeeper.Sdk.Abstractions.Nodes;

/// <summary>
/// Represents a class or type declaration node with specialized properties.
/// </summary>
public interface IClassNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the class.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the base class name, if any.
    /// </summary>
    string? BaseClassName { get; }

    /// <summary>
    /// Gets the implemented interface names.
    /// </summary>
    IEnumerable<string> ImplementedInterfaces { get; }

    /// <summary>
    /// Gets the methods declared in this class.
    /// </summary>
    IEnumerable<IMethodNode> Methods { get; }

    /// <summary>
    /// Gets the properties declared in this class.
    /// </summary>
    IEnumerable<IPropertyNode> Properties { get; }

    /// <summary>
    /// Gets the fields declared in this class.
    /// </summary>
    IEnumerable<IFieldNode> Fields { get; }

    /// <summary>
    /// Gets the constructors declared in this class.
    /// </summary>
    IEnumerable<IMethodNode> Constructors { get; }

    /// <summary>
    /// Gets the nested types declared in this class.
    /// </summary>
    IEnumerable<IClassNode> NestedTypes { get; }

    /// <summary>
    /// Gets whether this is a static class.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Gets whether this is an abstract class.
    /// </summary>
    bool IsAbstract { get; }

    /// <summary>
    /// Gets whether this is a sealed/final class.
    /// </summary>
    bool IsSealed { get; }

    /// <summary>
    /// Gets whether this is a partial class.
    /// </summary>
    bool IsPartial { get; }

    /// <summary>
    /// Gets whether this is a public class.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets whether this is a private class.
    /// </summary>
    bool IsPrivate { get; }

    /// <summary>
    /// Gets whether this is an internal class.
    /// </summary>
    bool IsInternal { get; }

    /// <summary>
    /// Gets the modifiers as a collection of strings.
    /// </summary>
    IEnumerable<string> Modifiers { get; }

    /// <summary>
    /// Gets the type parameters for generic classes.
    /// </summary>
    IEnumerable<string> TypeParameters { get; }

    /// <summary>
    /// Gets whether this is a generic class.
    /// </summary>
    bool IsGeneric => TypeParameters.Any();

    /// <summary>
    /// Gets whether this class has a base class.
    /// </summary>
    bool HasBaseClass => !string.IsNullOrEmpty(BaseClassName);
}

/// <summary>
/// Represents a property declaration node.
/// </summary>
public interface IPropertyNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type name of the property.
    /// </summary>
    string? TypeName { get; }

    /// <summary>
    /// Gets whether the property has a getter.
    /// </summary>
    bool HasGetter { get; }

    /// <summary>
    /// Gets whether the property has a setter.
    /// </summary>
    bool HasSetter { get; }

    /// <summary>
    /// Gets whether this is a public property.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets whether this is a private property.
    /// </summary>
    bool IsPrivate { get; }

    /// <summary>
    /// Gets whether this is a static property.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Gets whether this is an abstract property.
    /// </summary>
    bool IsAbstract { get; }

    /// <summary>
    /// Gets whether this is a virtual property.
    /// </summary>
    bool IsVirtual { get; }

    /// <summary>
    /// Gets whether this is an auto-implemented property.
    /// </summary>
    bool IsAutoProperty { get; }

    /// <summary>
    /// Gets the modifiers as a collection of strings.
    /// </summary>
    IEnumerable<string> Modifiers { get; }

    /// <summary>
    /// Gets the initializer expression, if any.
    /// </summary>
    IUnifiedSyntaxNode? Initializer { get; }
}

/// <summary>
/// Represents a field declaration node.
/// </summary>
public interface IFieldNode : IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type name of the field.
    /// </summary>
    string? TypeName { get; }

    /// <summary>
    /// Gets whether this is a public field.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets whether this is a private field.
    /// </summary>
    bool IsPrivate { get; }

    /// <summary>
    /// Gets whether this is a protected field.
    /// </summary>
    bool IsProtected { get; }

    /// <summary>
    /// Gets whether this is a static field.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Gets whether this is a readonly field.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets whether this is a constant field.
    /// </summary>
    bool IsConst { get; }

    /// <summary>
    /// Gets the modifiers as a collection of strings.
    /// </summary>
    IEnumerable<string> Modifiers { get; }

    /// <summary>
    /// Gets the initializer expression, if any.
    /// </summary>
    IUnifiedSyntaxNode? Initializer { get; }
}
