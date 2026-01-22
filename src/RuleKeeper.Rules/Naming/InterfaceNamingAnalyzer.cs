using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures interface names start with 'I' prefix.
/// </summary>
[Rule("CS-NAME-007",
    Name = "Interface Naming Convention",
    Description = "Interfaces must start with 'I' prefix",
    Severity = SeverityLevel.Medium,
    Category = "naming_conventions")]
public class InterfaceNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("pattern", Description = "Regex pattern for valid interface names", DefaultValue = @"^I[A-Z][a-zA-Z0-9]*$")]
    public string Pattern { get; set; } = @"^I[A-Z][a-zA-Z0-9]*$";

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var interfaces = root.DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>();

        var regex = new System.Text.RegularExpressions.Regex(Pattern);

        foreach (var iface in interfaces)
        {
            var name = iface.Identifier.Text;

            if (!regex.IsMatch(name))
            {
                string message = !name.StartsWith("I")
                    ? $"Interface '{name}' must start with 'I' prefix"
                    : $"Interface '{name}' must follow 'I' + PascalCase naming convention";

                yield return CreateViolation(
                    iface.Identifier.GetLocation(),
                    message,
                    context,
                    "Rename the interface to start with 'I' prefix (e.g., IMyInterface)"
                );
            }
        }
    }
}
