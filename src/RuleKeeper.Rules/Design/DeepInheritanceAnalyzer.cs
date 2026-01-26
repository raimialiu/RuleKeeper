using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects classes with deep inheritance hierarchies.
/// Deep inheritance makes code harder to understand and maintain.
/// </summary>
[Rule("CS-DESIGN-014",
    Name = "Deep Inheritance",
    Description = "Classes should not have deep inheritance hierarchies",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class DeepInheritanceAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_depth", Description = "Maximum inheritance depth", DefaultValue = 3)]
    public int MaxDepth { get; set; } = 3;

    [RuleParameter("ignore_system_types", Description = "Don't count System types in hierarchy", DefaultValue = true)]
    public bool IgnoreSystemTypes { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var semanticModel = context.SemanticModel;

        if (semanticModel == null)
            yield break;

        var classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classes)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (classSymbol == null)
                continue;

            var depth = CalculateInheritanceDepth(classSymbol);

            if (depth > MaxDepth)
            {
                yield return CreateViolation(
                    classDecl.Identifier.GetLocation(),
                    $"Class '{classDecl.Identifier.Text}' has inheritance depth of {depth} (max: {MaxDepth})",
                    context,
                    "Consider using composition over inheritance or flattening the hierarchy"
                );
            }
        }
    }

    private int CalculateInheritanceDepth(INamedTypeSymbol classSymbol)
    {
        var depth = 0;
        var current = classSymbol.BaseType;

        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            if (IgnoreSystemTypes && IsSystemType(current))
            {
                current = current.BaseType;
                continue;
            }

            depth++;
            current = current.BaseType;
        }

        return depth;
    }

    private static bool IsSystemType(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToString() ?? "";
        return ns.StartsWith("System") ||
               ns.StartsWith("Microsoft") ||
               ns.StartsWith("Windows");
    }
}
