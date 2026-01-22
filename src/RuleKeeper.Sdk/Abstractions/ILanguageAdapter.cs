namespace RuleKeeper.Sdk.Abstractions;

/// <summary>
/// Interface for language-specific parser adapters that convert language-specific ASTs
/// to the unified syntax tree representation.
/// </summary>
public interface ILanguageAdapter
{
    /// <summary>
    /// Gets the programming language this adapter handles.
    /// </summary>
    Language Language { get; }

    /// <summary>
    /// Gets the file extensions supported by this adapter (e.g., ".cs", ".py").
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Parses source code and returns a unified syntax tree.
    /// </summary>
    /// <param name="source">The source code to parse.</param>
    /// <param name="filePath">The file path of the source code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root node of the unified syntax tree.</returns>
    Task<IUnifiedSyntaxNode> ParseAsync(string source, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a type resolver for semantic analysis across multiple files.
    /// </summary>
    /// <param name="files">The files to include in the type resolution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A type resolver, or null if semantic analysis is not supported.</returns>
    Task<ITypeResolver?> CreateTypeResolverAsync(IEnumerable<string> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default include patterns for this language.
    /// </summary>
    IEnumerable<string> DefaultIncludePatterns { get; }

    /// <summary>
    /// Gets the default exclude patterns for this language.
    /// </summary>
    IEnumerable<string> DefaultExcludePatterns { get; }

    /// <summary>
    /// Determines if this adapter can handle the specified file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if this adapter can handle the file.</returns>
    bool CanHandle(string filePath);

    /// <summary>
    /// Gets the language-specific analysis context, if the adapter supports it.
    /// </summary>
    /// <param name="root">The unified syntax tree root.</param>
    /// <param name="source">The source text.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="typeResolver">Optional type resolver.</param>
    /// <returns>A language-specific analysis context, or null.</returns>
    object? GetNativeContext(IUnifiedSyntaxNode root, string source, string filePath, ITypeResolver? typeResolver);
}

/// <summary>
/// Interface for semantic type resolution across files.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Gets the type of a syntax node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>The type information, or null if unknown.</returns>
    ITypeInfo? GetType(IUnifiedSyntaxNode node);

    /// <summary>
    /// Gets the symbol for a syntax node (variable, method, type, etc.).
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>The symbol information, or null if unknown.</returns>
    ISymbolInfo? GetSymbol(IUnifiedSyntaxNode node);

    /// <summary>
    /// Determines if a type inherits from or implements another type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="baseTypeName">The base type or interface name.</param>
    /// <returns>True if the type inherits from or implements the base type.</returns>
    bool InheritsFrom(ITypeInfo type, string baseTypeName);

    /// <summary>
    /// Gets the programming language this resolver handles.
    /// </summary>
    Language Language { get; }
}

/// <summary>
/// Represents type information for a syntax node.
/// </summary>
public interface ITypeInfo
{
    /// <summary>
    /// Gets the name of the type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the fully qualified name of the type.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Gets whether this is a primitive/built-in type.
    /// </summary>
    bool IsPrimitive { get; }

    /// <summary>
    /// Gets whether this is a nullable type.
    /// </summary>
    bool IsNullable { get; }

    /// <summary>
    /// Gets whether this is an array type.
    /// </summary>
    bool IsArray { get; }

    /// <summary>
    /// Gets whether this is a generic type.
    /// </summary>
    bool IsGeneric { get; }

    /// <summary>
    /// Gets the element type for arrays or collections.
    /// </summary>
    ITypeInfo? ElementType { get; }

    /// <summary>
    /// Gets the type arguments for generic types.
    /// </summary>
    IReadOnlyList<ITypeInfo> TypeArguments { get; }

    /// <summary>
    /// Gets the base types and interfaces.
    /// </summary>
    IReadOnlyList<ITypeInfo> BaseTypes { get; }
}

/// <summary>
/// Represents symbol information for a syntax node.
/// </summary>
public interface ISymbolInfo
{
    /// <summary>
    /// Gets the name of the symbol.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the kind of symbol.
    /// </summary>
    SymbolKind Kind { get; }

    /// <summary>
    /// Gets the type of the symbol.
    /// </summary>
    ITypeInfo? Type { get; }

    /// <summary>
    /// Gets whether the symbol is public.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets whether the symbol is private.
    /// </summary>
    bool IsPrivate { get; }

    /// <summary>
    /// Gets whether the symbol is protected.
    /// </summary>
    bool IsProtected { get; }

    /// <summary>
    /// Gets whether the symbol is static.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// Gets whether the symbol is abstract.
    /// </summary>
    bool IsAbstract { get; }

    /// <summary>
    /// Gets whether the symbol is virtual.
    /// </summary>
    bool IsVirtual { get; }

    /// <summary>
    /// Gets whether the symbol is async (for methods).
    /// </summary>
    bool IsAsync { get; }

    /// <summary>
    /// Gets the containing type for members.
    /// </summary>
    ISymbolInfo? ContainingType { get; }

    /// <summary>
    /// Gets the declaring location of the symbol.
    /// </summary>
    SourceLocation? DeclaringLocation { get; }
}

/// <summary>
/// Kinds of symbols.
/// </summary>
public enum SymbolKind
{
    /// <summary>
    /// Unknown symbol kind.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A namespace or module.
    /// </summary>
    Namespace,

    /// <summary>
    /// A type (class, struct, enum, interface).
    /// </summary>
    Type,

    /// <summary>
    /// A method or function.
    /// </summary>
    Method,

    /// <summary>
    /// A property.
    /// </summary>
    Property,

    /// <summary>
    /// A field.
    /// </summary>
    Field,

    /// <summary>
    /// An event.
    /// </summary>
    Event,

    /// <summary>
    /// A local variable.
    /// </summary>
    Local,

    /// <summary>
    /// A parameter.
    /// </summary>
    Parameter,

    /// <summary>
    /// A type parameter.
    /// </summary>
    TypeParameter,

    /// <summary>
    /// A label.
    /// </summary>
    Label,

    /// <summary>
    /// An alias.
    /// </summary>
    Alias
}
