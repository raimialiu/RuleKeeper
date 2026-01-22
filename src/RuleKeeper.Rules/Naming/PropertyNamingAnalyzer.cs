using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures property names follow PascalCase convention.
/// </summary>
[Rule("CS-NAME-006",
    Name = "Property Naming Convention",
    Description = "Properties must use PascalCase naming",
    Severity = SeverityLevel.Medium,
    Category = "naming_conventions")]
public class PropertyNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("pattern", Description = "Regex pattern for valid property names", DefaultValue = @"^[A-Z][a-zA-Z0-9]*$")]
    public string Pattern { get; set; } = @"^[A-Z][a-zA-Z0-9]*$";

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var properties = root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>();

        var regex = new System.Text.RegularExpressions.Regex(Pattern);

        foreach (var property in properties)
        {
            var name = property.Identifier.Text;

            // Skip explicit interface implementations
            if (property.ExplicitInterfaceSpecifier != null)
                continue;

            if (!regex.IsMatch(name))
            {
                yield return CreateViolation(
                    property.Identifier.GetLocation(),
                    $"Property '{name}' must use PascalCase naming convention",
                    context,
                    "Rename the property to use PascalCase (e.g., MyProperty)"
                );
            }
        }
    }
}
