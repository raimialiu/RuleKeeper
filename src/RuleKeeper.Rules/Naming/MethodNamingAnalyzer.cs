using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures method names follow PascalCase convention.
/// </summary>
[Rule("CS-NAME-002",
    Name = "Method Naming Convention",
    Description = "Methods must use PascalCase naming",
    Severity = SeverityLevel.Medium,
    Category = "naming_conventions")]
public class MethodNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("pattern", Description = "Regex pattern for valid method names", DefaultValue = @"^[A-Z][a-zA-Z0-9]*$")]
    public string Pattern { get; set; } = @"^[A-Z][a-zA-Z0-9]*$";

    [RuleParameter("exclude_private", Description = "Exclude private methods from check", DefaultValue = false)]
    public bool ExcludePrivate { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        var regex = new System.Text.RegularExpressions.Regex(Pattern);

        foreach (var method in methods)
        {
            // Skip private methods if configured
            if (ExcludePrivate && method.Modifiers.Any(SyntaxKind.PrivateKeyword))
                continue;

            var name = method.Identifier.Text;

            // Skip special methods
            if (IsSpecialMethod(name))
                continue;

            if (!regex.IsMatch(name))
            {
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{name}' must use PascalCase naming convention",
                    context,
                    "Rename the method to use PascalCase (e.g., GetValue)"
                );
            }
        }
    }

    private static bool IsSpecialMethod(string name)
    {
        // Skip operators and special method patterns
        return name.StartsWith("op_") ||
               name.StartsWith("get_") ||
               name.StartsWith("set_") ||
               name.StartsWith("add_") ||
               name.StartsWith("remove_");
    }
}
