namespace RuleKeeper.Sdk.Abstractions;

/// <summary>
/// Base implementation of <see cref="IUnifiedSyntaxNode"/> that provides common functionality.
/// Language adapters can extend this class to provide their own implementations.
/// </summary>
public abstract class UnifiedSyntaxNodeBase : IUnifiedSyntaxNode
{
    /// <inheritdoc />
    public abstract UnifiedSyntaxKind Kind { get; }

    /// <inheritdoc />
    public abstract SourceLocation Location { get; }

    /// <inheritdoc />
    public abstract string Text { get; }

    /// <inheritdoc />
    public abstract IUnifiedSyntaxNode? Parent { get; }

    /// <inheritdoc />
    public abstract IEnumerable<IUnifiedSyntaxNode> Children { get; }

    /// <inheritdoc />
    public abstract object? NativeNode { get; }

    /// <inheritdoc />
    public abstract Language Language { get; }

    /// <inheritdoc />
    public virtual bool ContainsErrors => false;

    /// <inheritdoc />
    public virtual IEnumerable<IUnifiedSyntaxNode> Descendants()
    {
        var stack = new Stack<IUnifiedSyntaxNode>();
        foreach (var child in Children.Reverse())
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            foreach (var child in current.Children.Reverse())
            {
                stack.Push(child);
            }
        }
    }

    /// <inheritdoc />
    public virtual IEnumerable<IUnifiedSyntaxNode> DescendantsOfKind(UnifiedSyntaxKind kind)
    {
        return Descendants().Where(n => n.Kind == kind);
    }

    /// <inheritdoc />
    public virtual IEnumerable<IUnifiedSyntaxNode> DescendantsOfKind(params UnifiedSyntaxKind[] kinds)
    {
        var kindSet = new HashSet<UnifiedSyntaxKind>(kinds);
        return Descendants().Where(n => kindSet.Contains(n.Kind));
    }

    /// <inheritdoc />
    public virtual IEnumerable<IUnifiedSyntaxNode> Ancestors()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <inheritdoc />
    public virtual IUnifiedSyntaxNode? FirstAncestorOfKind(UnifiedSyntaxKind kind)
    {
        return Ancestors().FirstOrDefault(n => n.Kind == kind);
    }

    /// <inheritdoc />
    public virtual IUnifiedSyntaxNode? FirstChildOfKind(UnifiedSyntaxKind kind)
    {
        return Children.FirstOrDefault(n => n.Kind == kind);
    }

    /// <inheritdoc />
    public virtual IUnifiedSyntaxNode? FirstDescendantOfKind(UnifiedSyntaxKind kind)
    {
        return Descendants().FirstOrDefault(n => n.Kind == kind);
    }

    /// <inheritdoc />
    public bool IsKind(UnifiedSyntaxKind kind) => Kind == kind;

    /// <inheritdoc />
    public bool IsKind(params UnifiedSyntaxKind[] kinds) => kinds.Contains(Kind);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Kind} at {Location}";
    }
}
