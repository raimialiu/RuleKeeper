using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects boolean parameters which often indicate methods doing
/// multiple things. Consider using enums or separate methods instead.
/// </summary>
[Rule("CS-DESIGN-015",
    Name = "Boolean Parameters",
    Description = "Avoid boolean parameters; consider enums or separate methods",
    Severity = SeverityLevel.Info,
    Category = "design")]
public class BooleanParametersAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_bool_params", Description = "Maximum boolean parameters allowed", DefaultValue = 1)]
    public int MaxBoolParams { get; set; } = 1;

    [RuleParameter("ignore_private", Description = "Ignore private methods", DefaultValue = true)]
    public bool IgnorePrivate { get; set; } = true;

    [RuleParameter("allow_common_names", Description = "Allow common boolean parameter names", DefaultValue = true)]
    public bool AllowCommonNames { get; set; } = true;

    private static readonly HashSet<string> CommonBoolNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "enabled", "disabled", "visible", "hidden", "active", "inactive",
        "async", "sync", "recursive", "force", "overwrite", "append"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Skip private methods if configured
            if (IgnorePrivate && method.Modifiers.Any(SyntaxKind.PrivateKeyword))
                continue;

            var boolParams = method.ParameterList.Parameters
                .Where(p => IsBooleanType(p.Type))
                .Where(p => !AllowCommonNames || !CommonBoolNames.Contains(p.Identifier.Text))
                .ToList();

            if (boolParams.Count > MaxBoolParams)
            {
                var paramNames = string.Join(", ", boolParams.Select(p => p.Identifier.Text));
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{method.Identifier.Text}' has {boolParams.Count} boolean parameters ({paramNames})",
                    context,
                    "Consider using an enum, options object, or separate methods"
                );
            }
        }
    }

    private static bool IsBooleanType(TypeSyntax? type)
    {
        if (type == null)
            return false;

        var typeName = type.ToString().ToLowerInvariant();
        return typeName == "bool" || typeName == "boolean" || typeName == "system.boolean";
    }
}
