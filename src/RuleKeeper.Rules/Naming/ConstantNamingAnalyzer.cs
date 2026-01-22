using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures constant names follow naming conventions.
/// </summary>
[Rule("CS-NAME-005",
    Name = "Constant Naming Convention",
    Description = "Constants must use PascalCase or UPPER_CASE naming",
    Severity = SeverityLevel.Low,
    Category = "naming_conventions")]
public class ConstantNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("style", Description = "Naming style: PascalCase or UPPER_CASE", DefaultValue = "PascalCase")]
    public string Style { get; set; } = "PascalCase";

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var constants = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(SyntaxKind.ConstKeyword));

        var pattern = Style.Equals("UPPER_CASE", StringComparison.OrdinalIgnoreCase)
            ? @"^[A-Z][A-Z0-9_]*$"
            : @"^[A-Z][a-zA-Z0-9]*$";

        var regex = new System.Text.RegularExpressions.Regex(pattern);

        foreach (var field in constants)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                var name = variable.Identifier.Text;

                if (!regex.IsMatch(name))
                {
                    var hint = Style.Equals("UPPER_CASE", StringComparison.OrdinalIgnoreCase)
                        ? "Rename to use UPPER_CASE (e.g., MAX_VALUE)"
                        : "Rename to use PascalCase (e.g., MaxValue)";

                    yield return CreateViolation(
                        variable.Identifier.GetLocation(),
                        $"Constant '{name}' must use {Style} naming convention",
                        context,
                        hint
                    );
                }
            }
        }
    }
}
