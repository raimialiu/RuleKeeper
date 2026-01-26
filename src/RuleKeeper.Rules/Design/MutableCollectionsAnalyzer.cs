using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects public properties or fields that expose mutable collections.
/// Exposing mutable collections breaks encapsulation.
/// </summary>
[Rule("CS-DESIGN-016",
    Name = "Mutable Collections",
    Description = "Public properties should not expose mutable collections",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class MutableCollectionsAnalyzer : BaseRuleAnalyzer
{
    private static readonly HashSet<string> MutableCollectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "List", "Dictionary", "HashSet", "Queue", "Stack",
        "SortedList", "SortedSet", "SortedDictionary",
        "LinkedList", "ObservableCollection",
        "List`1", "Dictionary`2", "HashSet`1"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        // Check public properties
        var properties = root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(SyntaxKind.PublicKeyword));

        foreach (var property in properties)
        {
            if (IsMutableCollectionType(property.Type))
            {
                // Check if it has a private/protected setter or is read-only
                var hasPublicSetter = property.AccessorList?.Accessors
                    .Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) &&
                              !a.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) ||
                                                     m.IsKind(SyntaxKind.ProtectedKeyword)));

                yield return CreateViolation(
                    property.Identifier.GetLocation(),
                    $"Property '{property.Identifier.Text}' exposes mutable collection type '{property.Type}'",
                    context,
                    "Return IReadOnlyList<T>, IReadOnlyCollection<T>, or IEnumerable<T> instead"
                );
            }
        }

        // Check public fields (these should already be flagged by PublicFieldsAnalyzer)
        var fields = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(SyntaxKind.PublicKeyword));

        foreach (var field in fields)
        {
            if (IsMutableCollectionType(field.Declaration.Type))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    yield return CreateViolation(
                        variable.GetLocation(),
                        $"Field '{variable.Identifier.Text}' exposes mutable collection type",
                        context,
                        "Use a property with IReadOnlyList<T> or IReadOnlyCollection<T>"
                    );
                }
            }
        }
    }

    private static bool IsMutableCollectionType(TypeSyntax? type)
    {
        if (type == null)
            return false;

        var typeName = type.ToString();

        // Extract the base type name (before generic parameters)
        var baseTypeName = typeName.Split('<')[0].Split('.').Last();

        return MutableCollectionTypes.Contains(baseTypeName);
    }
}
