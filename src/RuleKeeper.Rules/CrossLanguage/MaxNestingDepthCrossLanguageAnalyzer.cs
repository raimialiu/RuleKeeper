using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that checks for code with too many levels of nesting.
/// </summary>
[Rule("XL-DESIGN-004",
    Name = "Max Nesting Depth (Cross-Language)",
    Description = "Code should not have too many levels of nesting",
    Severity = SeverityLevel.Medium,
    Category = "design")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class MaxNestingDepthCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    [RuleParameter("max_depth", Description = "Maximum nesting depth allowed", DefaultValue = 3)]
    public int MaxDepth { get; set; } = 3;

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var methods = context.GetMethods();

        foreach (var method in methods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var violations = CheckNestingDepth(method, 0, context);
            foreach (var violation in violations)
            {
                yield return violation;
            }
        }
    }

    private IEnumerable<Violation> CheckNestingDepth(IUnifiedSyntaxNode node, int currentDepth, UnifiedAnalysisContext context)
    {
        foreach (var child in node.Children)
        {
            var newDepth = currentDepth;
            var isNestingNode = false;

            if (child.Kind is UnifiedSyntaxKind.IfStatement or
                UnifiedSyntaxKind.ForStatement or
                UnifiedSyntaxKind.ForEachStatement or
                UnifiedSyntaxKind.WhileStatement or
                UnifiedSyntaxKind.DoWhileStatement or
                UnifiedSyntaxKind.SwitchStatement or
                UnifiedSyntaxKind.TryStatement)
            {
                isNestingNode = true;
                newDepth++;
            }

            if (newDepth > MaxDepth && isNestingNode)
            {
                yield return CreateViolation(
                    child,
                    $"Nesting depth of {newDepth} exceeds maximum of {MaxDepth}",
                    context,
                    "Consider extracting nested code into separate functions or using early returns"
                );
            }

            foreach (var violation in CheckNestingDepth(child, newDepth, context))
            {
                yield return violation;
            }
        }
    }
}
