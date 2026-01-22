namespace RuleKeeper.Sdk.Abstractions;

/// <summary>
/// Represents a language-agnostic syntax node in the abstract syntax tree.
/// This interface provides a unified way to traverse and analyze code across different programming languages.
/// </summary>
public interface IUnifiedSyntaxNode
{
    /// <summary>
    /// Gets the kind of this syntax node.
    /// </summary>
    UnifiedSyntaxKind Kind { get; }

    /// <summary>
    /// Gets the source location of this node.
    /// </summary>
    SourceLocation Location { get; }

    /// <summary>
    /// Gets the source text of this node.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the parent node, or null if this is the root.
    /// </summary>
    IUnifiedSyntaxNode? Parent { get; }

    /// <summary>
    /// Gets the immediate child nodes.
    /// </summary>
    IEnumerable<IUnifiedSyntaxNode> Children { get; }

    /// <summary>
    /// Gets all descendant nodes.
    /// </summary>
    /// <returns>An enumerable of all descendant nodes.</returns>
    IEnumerable<IUnifiedSyntaxNode> Descendants();

    /// <summary>
    /// Gets all descendant nodes of a specific kind.
    /// </summary>
    /// <param name="kind">The kind to filter by.</param>
    /// <returns>An enumerable of matching descendant nodes.</returns>
    IEnumerable<IUnifiedSyntaxNode> DescendantsOfKind(UnifiedSyntaxKind kind);

    /// <summary>
    /// Gets all descendant nodes of specific kinds.
    /// </summary>
    /// <param name="kinds">The kinds to filter by.</param>
    /// <returns>An enumerable of matching descendant nodes.</returns>
    IEnumerable<IUnifiedSyntaxNode> DescendantsOfKind(params UnifiedSyntaxKind[] kinds);

    /// <summary>
    /// Gets the underlying native parser node (e.g., Roslyn SyntaxNode, ANTLR ParseTree).
    /// </summary>
    object? NativeNode { get; }

    /// <summary>
    /// Gets the programming language of this node.
    /// </summary>
    Language Language { get; }

    /// <summary>
    /// Gets whether this node contains any syntax errors.
    /// </summary>
    bool ContainsErrors { get; }

    /// <summary>
    /// Gets the ancestor nodes from this node up to the root.
    /// </summary>
    /// <returns>An enumerable of ancestor nodes.</returns>
    IEnumerable<IUnifiedSyntaxNode> Ancestors();

    /// <summary>
    /// Gets the first ancestor of a specific kind.
    /// </summary>
    /// <param name="kind">The kind to search for.</param>
    /// <returns>The first matching ancestor, or null if not found.</returns>
    IUnifiedSyntaxNode? FirstAncestorOfKind(UnifiedSyntaxKind kind);

    /// <summary>
    /// Gets the first child of a specific kind.
    /// </summary>
    /// <param name="kind">The kind to search for.</param>
    /// <returns>The first matching child, or null if not found.</returns>
    IUnifiedSyntaxNode? FirstChildOfKind(UnifiedSyntaxKind kind);

    /// <summary>
    /// Gets the first descendant of a specific kind.
    /// </summary>
    /// <param name="kind">The kind to search for.</param>
    /// <returns>The first matching descendant, or null if not found.</returns>
    IUnifiedSyntaxNode? FirstDescendantOfKind(UnifiedSyntaxKind kind);

    /// <summary>
    /// Determines if this node is of a specific kind.
    /// </summary>
    /// <param name="kind">The kind to check.</param>
    /// <returns>True if this node is of the specified kind.</returns>
    bool IsKind(UnifiedSyntaxKind kind);

    /// <summary>
    /// Determines if this node is of any of the specified kinds.
    /// </summary>
    /// <param name="kinds">The kinds to check.</param>
    /// <returns>True if this node is of any of the specified kinds.</returns>
    bool IsKind(params UnifiedSyntaxKind[] kinds);
}

/// <summary>
/// Extension methods for <see cref="IUnifiedSyntaxNode"/>.
/// </summary>
public static class UnifiedSyntaxNodeExtensions
{
    /// <summary>
    /// Gets all descendants of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of node to filter.</typeparam>
    /// <param name="node">The starting node.</param>
    /// <returns>An enumerable of matching descendant nodes.</returns>
    public static IEnumerable<T> DescendantsOfType<T>(this IUnifiedSyntaxNode node) where T : class, IUnifiedSyntaxNode
    {
        return node.Descendants().OfType<T>();
    }

    /// <summary>
    /// Gets the depth of this node in the tree.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The depth (0 for root).</returns>
    public static int GetDepth(this IUnifiedSyntaxNode node)
    {
        int depth = 0;
        var current = node.Parent;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }

    /// <summary>
    /// Gets all sibling nodes of this node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>An enumerable of sibling nodes.</returns>
    public static IEnumerable<IUnifiedSyntaxNode> Siblings(this IUnifiedSyntaxNode node)
    {
        if (node.Parent == null)
            return Enumerable.Empty<IUnifiedSyntaxNode>();

        return node.Parent.Children.Where(c => c != node);
    }

    /// <summary>
    /// Gets the next sibling of this node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The next sibling, or null if not found.</returns>
    public static IUnifiedSyntaxNode? NextSibling(this IUnifiedSyntaxNode node)
    {
        if (node.Parent == null)
            return null;

        var children = node.Parent.Children.ToList();
        var index = children.IndexOf(node);
        return index >= 0 && index < children.Count - 1 ? children[index + 1] : null;
    }

    /// <summary>
    /// Gets the previous sibling of this node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The previous sibling, or null if not found.</returns>
    public static IUnifiedSyntaxNode? PreviousSibling(this IUnifiedSyntaxNode node)
    {
        if (node.Parent == null)
            return null;

        var children = node.Parent.Children.ToList();
        var index = children.IndexOf(node);
        return index > 0 ? children[index - 1] : null;
    }

    /// <summary>
    /// Determines if this node is an ancestor of another node.
    /// </summary>
    /// <param name="node">The potential ancestor.</param>
    /// <param name="other">The potential descendant.</param>
    /// <returns>True if this node is an ancestor of the other node.</returns>
    public static bool IsAncestorOf(this IUnifiedSyntaxNode node, IUnifiedSyntaxNode other)
    {
        return other.Ancestors().Contains(node);
    }

    /// <summary>
    /// Determines if this node is a descendant of another node.
    /// </summary>
    /// <param name="node">The potential descendant.</param>
    /// <param name="other">The potential ancestor.</param>
    /// <returns>True if this node is a descendant of the other node.</returns>
    public static bool IsDescendantOf(this IUnifiedSyntaxNode node, IUnifiedSyntaxNode other)
    {
        return node.Ancestors().Contains(other);
    }

    /// <summary>
    /// Gets all class declarations from this node.
    /// </summary>
    public static IEnumerable<IUnifiedSyntaxNode> GetClasses(this IUnifiedSyntaxNode node) =>
        node.DescendantsOfKind(UnifiedSyntaxKind.ClassDeclaration);

    /// <summary>
    /// Gets all method declarations from this node.
    /// </summary>
    public static IEnumerable<IUnifiedSyntaxNode> GetMethods(this IUnifiedSyntaxNode node) =>
        node.DescendantsOfKind(UnifiedSyntaxKind.MethodDeclaration);

    /// <summary>
    /// Gets all interface declarations from this node.
    /// </summary>
    public static IEnumerable<IUnifiedSyntaxNode> GetInterfaces(this IUnifiedSyntaxNode node) =>
        node.DescendantsOfKind(UnifiedSyntaxKind.InterfaceDeclaration);

    /// <summary>
    /// Gets all property declarations from this node.
    /// </summary>
    public static IEnumerable<IUnifiedSyntaxNode> GetProperties(this IUnifiedSyntaxNode node) =>
        node.DescendantsOfKind(UnifiedSyntaxKind.PropertyDeclaration);

    /// <summary>
    /// Gets all field declarations from this node.
    /// </summary>
    public static IEnumerable<IUnifiedSyntaxNode> GetFields(this IUnifiedSyntaxNode node) =>
        node.DescendantsOfKind(UnifiedSyntaxKind.FieldDeclaration);

    /// <summary>
    /// Gets all invocation expressions from this node.
    /// </summary>
    public static IEnumerable<IUnifiedSyntaxNode> GetInvocations(this IUnifiedSyntaxNode node) =>
        node.DescendantsOfKind(UnifiedSyntaxKind.InvocationExpression);
}
