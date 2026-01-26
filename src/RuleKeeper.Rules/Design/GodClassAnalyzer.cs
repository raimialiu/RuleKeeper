using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects "God Classes" that have too many methods, fields, or responsibilities.
/// These classes violate the Single Responsibility Principle.
/// </summary>
[Rule("CS-DESIGN-011",
    Name = "God Class",
    Description = "Classes should not have too many methods or fields",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class GodClassAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_methods", Description = "Maximum number of methods in a class", DefaultValue = 20)]
    public int MaxMethods { get; set; } = 20;

    [RuleParameter("max_fields", Description = "Maximum number of fields in a class", DefaultValue = 15)]
    public int MaxFields { get; set; } = 15;

    [RuleParameter("max_properties", Description = "Maximum number of properties in a class", DefaultValue = 20)]
    public int MaxProperties { get; set; } = 20;

    [RuleParameter("count_inherited", Description = "Count inherited members", DefaultValue = false)]
    public bool CountInherited { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classes)
        {
            var methodCount = classDecl.Members.OfType<MethodDeclarationSyntax>().Count();
            var fieldCount = classDecl.Members.OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables).Count();
            var propertyCount = classDecl.Members.OfType<PropertyDeclarationSyntax>().Count();

            var violations = new List<string>();

            if (methodCount > MaxMethods)
            {
                violations.Add($"{methodCount} methods (max: {MaxMethods})");
            }

            if (fieldCount > MaxFields)
            {
                violations.Add($"{fieldCount} fields (max: {MaxFields})");
            }

            if (propertyCount > MaxProperties)
            {
                violations.Add($"{propertyCount} properties (max: {MaxProperties})");
            }

            if (violations.Count > 0)
            {
                yield return CreateViolation(
                    classDecl.Identifier.GetLocation(),
                    $"Class '{classDecl.Identifier.Text}' may be a God Class: {string.Join(", ", violations)}",
                    context,
                    "Consider splitting this class into smaller, focused classes following Single Responsibility Principle"
                );
            }
        }
    }
}
