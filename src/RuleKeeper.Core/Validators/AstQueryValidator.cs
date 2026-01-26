using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Validator that queries AST nodes declaratively based on YAML configuration.
/// </summary>
public class AstQueryValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _name;
    private readonly string? _description;
    private readonly AstQueryConfig _query;
    private readonly string? _defaultMessage;

    public override string ValidatorId => _validatorId;
    public override string Name => _name;
    public override string? Description => _description;

    public AstQueryValidator(string validatorId, string name, string? description, AstQueryConfig query, string? defaultMessage = null)
    {
        _validatorId = validatorId;
        _name = name;
        _description = description;
        _query = query;
        _defaultMessage = defaultMessage;
    }

    public override Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();

        // Check language filter
        if (_query.Languages.Count > 0 &&
            !_query.Languages.Any(l => l.Equals(context.Language.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult<IEnumerable<Violation>>(violations);
        }

        // Use unified AST if available, otherwise try Roslyn
        if (context.UnifiedRoot != null)
        {
            violations.AddRange(ValidateUnifiedAst(context, context.UnifiedRoot, cancellationToken));
        }
        else if (context.RoslynSyntaxTree != null)
        {
            violations.AddRange(ValidateRoslynAst(context, cancellationToken));
        }

        return Task.FromResult<IEnumerable<Violation>>(violations);
    }

    private IEnumerable<Violation> ValidateUnifiedAst(ValidationContext context, IUnifiedSyntaxNode root, CancellationToken cancellationToken)
    {
        var violations = new List<Violation>();
        var candidates = FindMatchingNodes(root, cancellationToken);

        foreach (var node in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchesQuery(node))
            {
                var message = context.CustomMessage ?? _defaultMessage ?? $"AST query matched: {node.Kind}";
                violations.Add(CreateViolationFromNode(context, node, message));
            }
        }

        return violations;
    }

    private IEnumerable<IUnifiedSyntaxNode> FindMatchingNodes(IUnifiedSyntaxNode root, CancellationToken cancellationToken)
    {
        if (_query.NodeKinds.Count == 0)
        {
            // No node kind filter - return all nodes
            return root.Descendants();
        }

        // Filter by node kinds
        return root.Descendants()
            .Where(n => _query.NodeKinds.Any(k =>
                k.Equals(n.Kind.ToString(), StringComparison.OrdinalIgnoreCase)));
    }

    private bool MatchesQuery(IUnifiedSyntaxNode node)
    {
        // Check properties
        if (_query.Properties != null && _query.Properties.Count > 0)
        {
            if (!MatchesProperties(node, _query.Properties))
                return false;
        }

        // Check parent
        if (_query.Parent != null)
        {
            var parent = node.Parent;
            if (parent == null || !MatchesParentQuery(parent, _query.Parent))
                return false;
        }

        // Check has_children
        if (_query.HasChildren.Count > 0)
        {
            var childKinds = node.Children.Select(c => c.Kind.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!_query.HasChildren.All(hc => childKinds.Contains(hc)))
                return false;
        }

        // Check no_children
        if (_query.NoChildren.Count > 0)
        {
            var childKinds = node.Children.Select(c => c.Kind.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (_query.NoChildren.Any(nc => childKinds.Contains(nc)))
                return false;
        }

        // Check has_ancestor
        if (_query.HasAncestor.Count > 0)
        {
            var ancestorKinds = node.Ancestors().Select(a => a.Kind.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!_query.HasAncestor.All(ha => ancestorKinds.Contains(ha)))
                return false;
        }

        // Check no_ancestor
        if (_query.NoAncestor.Count > 0)
        {
            var ancestorKinds = node.Ancestors().Select(a => a.Kind.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (_query.NoAncestor.Any(na => ancestorKinds.Contains(na)))
                return false;
        }

        // Check text_matches
        if (!string.IsNullOrEmpty(_query.TextMatches))
        {
            var regex = new Regex(_query.TextMatches, RegexOptions.IgnoreCase);
            if (!regex.IsMatch(node.Text ?? ""))
                return false;
        }

        // Check text_not_matches
        if (!string.IsNullOrEmpty(_query.TextNotMatches))
        {
            var regex = new Regex(_query.TextNotMatches, RegexOptions.IgnoreCase);
            if (regex.IsMatch(node.Text ?? ""))
                return false;
        }

        // Check name_matches (for named nodes)
        if (!string.IsNullOrEmpty(_query.NameMatches))
        {
            var name = GetNodeName(node);
            if (name == null)
                return false;

            var regex = new Regex(_query.NameMatches, RegexOptions.IgnoreCase);
            if (!regex.IsMatch(name))
                return false;
        }

        // Check name_not_matches
        if (!string.IsNullOrEmpty(_query.NameNotMatches))
        {
            var name = GetNodeName(node);
            if (name != null)
            {
                var regex = new Regex(_query.NameNotMatches, RegexOptions.IgnoreCase);
                if (regex.IsMatch(name))
                    return false;
            }
        }

        // Check depth constraints
        var depth = node.GetDepth();
        if (_query.MinDepth.HasValue && depth < _query.MinDepth.Value)
            return false;
        if (_query.MaxDepth.HasValue && depth > _query.MaxDepth.Value)
            return false;

        // Check attributes (if supported by the node)
        if (_query.HasAttribute.Count > 0)
        {
            var attributes = GetNodeAttributes(node);
            if (!_query.HasAttribute.All(a => attributes.Contains(a, StringComparer.OrdinalIgnoreCase)))
                return false;
        }

        if (_query.NoAttribute.Count > 0)
        {
            var attributes = GetNodeAttributes(node);
            if (_query.NoAttribute.Any(a => attributes.Contains(a, StringComparer.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    private bool MatchesParentQuery(IUnifiedSyntaxNode parent, AstQueryConfig parentQuery)
    {
        // Check node kind
        if (parentQuery.NodeKinds.Count > 0 &&
            !parentQuery.NodeKinds.Any(k => k.Equals(parent.Kind.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Check properties
        if (parentQuery.Properties != null && !MatchesProperties(parent, parentQuery.Properties))
        {
            return false;
        }

        // Recursively check parent's parent
        if (parentQuery.Parent != null)
        {
            var grandParent = parent.Parent;
            if (grandParent == null || !MatchesParentQuery(grandParent, parentQuery.Parent))
                return false;
        }

        return true;
    }

    private bool MatchesProperties(IUnifiedSyntaxNode node, Dictionary<string, object> properties)
    {
        var nodeType = node.GetType();

        foreach (var (propName, expectedValue) in properties)
        {
            var property = nodeType.GetProperty(propName);
            if (property == null)
            {
                // Try interface properties
                property = node.GetType().GetInterfaces()
                    .SelectMany(i => i.GetProperties())
                    .FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            }

            if (property == null)
                return false;

            var actualValue = property.GetValue(node);
            if (!ValuesMatch(actualValue, expectedValue))
                return false;
        }

        return true;
    }

    private bool ValuesMatch(object? actual, object expected)
    {
        if (actual == null)
            return expected == null || expected.ToString() == "null";

        if (expected is bool expectedBool)
        {
            if (actual is bool actualBool)
                return actualBool == expectedBool;
            return bool.TryParse(actual.ToString(), out var parsed) && parsed == expectedBool;
        }

        if (expected is int expectedInt)
        {
            if (actual is int actualInt)
                return actualInt == expectedInt;
            return int.TryParse(actual.ToString(), out var parsed) && parsed == expectedInt;
        }

        if (expected is string expectedStr)
        {
            return actual.ToString()?.Equals(expectedStr, StringComparison.OrdinalIgnoreCase) == true;
        }

        return actual.Equals(expected);
    }

    private string? GetNodeName(IUnifiedSyntaxNode node)
    {
        // Try common name properties
        var nameProperty = node.GetType().GetProperty("Name")
            ?? node.GetType().GetProperty("Identifier");

        if (nameProperty != null)
        {
            var value = nameProperty.GetValue(node);
            return value?.ToString();
        }

        // Try interface-based name access
        if (node is Sdk.Abstractions.Nodes.IMethodNode methodNode)
            return methodNode.Name;
        if (node is Sdk.Abstractions.Nodes.IClassNode classNode)
            return classNode.Name;

        return null;
    }

    private HashSet<string> GetNodeAttributes(IUnifiedSyntaxNode node)
    {
        var attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Try to get attributes/annotations from the node
        var attributesProperty = node.GetType().GetProperty("Attributes")
            ?? node.GetType().GetProperty("Annotations");

        if (attributesProperty != null)
        {
            var value = attributesProperty.GetValue(node);
            if (value is IEnumerable<object> enumerable)
            {
                foreach (var attr in enumerable)
                {
                    var name = attr.GetType().GetProperty("Name")?.GetValue(attr)?.ToString();
                    if (name != null)
                        attributes.Add(name);
                }
            }
        }

        return attributes;
    }

    private Violation CreateViolationFromNode(ValidationContext context, IUnifiedSyntaxNode node, string message)
    {
        var location = node.Location;

        return CreateViolation(
            context,
            message,
            location.StartLine,
            location.StartColumn,
            location.EndLine,
            location.EndColumn,
            context.GetLine(location.StartLine)?.TrimEnd('\r', '\n'));
    }

    private IEnumerable<Violation> ValidateRoslynAst(ValidationContext context, CancellationToken cancellationToken)
    {
        // For Roslyn-based validation, we'd need to convert the query to Roslyn-specific logic
        // This is a simplified implementation that handles common cases
        var violations = new List<Violation>();
        var root = context.RoslynSyntaxTree!.GetRoot(cancellationToken);

        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeKind = node.Kind().ToString();

            // Check node kind match
            if (_query.NodeKinds.Count > 0 &&
                !_query.NodeKinds.Any(k => nodeKind.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Check text matches
            if (!string.IsNullOrEmpty(_query.TextMatches))
            {
                var regex = new Regex(_query.TextMatches, RegexOptions.IgnoreCase);
                if (!regex.IsMatch(node.ToString()))
                    continue;
            }

            if (!string.IsNullOrEmpty(_query.TextNotMatches))
            {
                var regex = new Regex(_query.TextNotMatches, RegexOptions.IgnoreCase);
                if (regex.IsMatch(node.ToString()))
                    continue;
            }

            // Check has_children
            if (_query.HasChildren.Count > 0)
            {
                var childKinds = node.ChildNodes().Select(c => c.Kind().ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!_query.HasChildren.All(hc => childKinds.Any(ck => ck.Contains(hc, StringComparison.OrdinalIgnoreCase))))
                    continue;
            }

            // Check no_children
            if (_query.NoChildren.Count > 0)
            {
                var childKinds = node.ChildNodes().Select(c => c.Kind().ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (_query.NoChildren.Any(nc => childKinds.Any(ck => ck.Contains(nc, StringComparison.OrdinalIgnoreCase))))
                    continue;
            }

            // Add violation
            var location = node.GetLocation();
            var lineSpan = location.GetLineSpan();
            var message = context.CustomMessage ?? _defaultMessage ?? $"AST query matched: {nodeKind}";

            violations.Add(CreateViolation(
                context,
                message,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                lineSpan.EndLinePosition.Line + 1,
                lineSpan.EndLinePosition.Character + 1,
                context.GetLine(lineSpan.StartLinePosition.Line + 1)?.TrimEnd('\r', '\n')));
        }

        return violations;
    }
}
