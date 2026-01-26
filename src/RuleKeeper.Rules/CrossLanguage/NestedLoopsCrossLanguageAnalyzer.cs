using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that detects nested loops which often indicate
/// O(n²) or worse algorithmic complexity.
/// </summary>
[Rule("XL-DESIGN-005",
    Name = "Nested Loops (Cross-Language)",
    Description = "Avoid nested loops which can cause performance issues",
    Severity = SeverityLevel.High,
    Category = "design")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class NestedLoopsCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    [RuleParameter("max_nested_loops", Description = "Maximum allowed nested loops (1 = no nesting)", DefaultValue = 1)]
    public int MaxNestedLoops { get; set; } = 1;

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var methods = context.GetMethods();

        foreach (var method in methods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var topLevelLoops = method.Children
                .Where(IsLoopNode)
                .ToList();

            foreach (var loop in topLevelLoops)
            {
                var violations = CheckNestedLoops(loop, 1, context);
                foreach (var violation in violations)
                {
                    yield return violation;
                }
            }
        }
    }

    private IEnumerable<Violation> CheckNestedLoops(IUnifiedSyntaxNode loop, int depth, UnifiedAnalysisContext context)
    {
        var nestedLoops = loop.Descendants()
            .Where(IsLoopNode)
            .Where(n => GetParentLoop(n, loop) == loop);

        foreach (var nestedLoop in nestedLoops)
        {
            var newDepth = depth + 1;

            if (newDepth > MaxNestedLoops)
            {
                var loopType = GetLoopType(nestedLoop);
                yield return CreateViolation(
                    nestedLoop,
                    $"Nested {loopType} detected (depth: {newDepth}, max: {MaxNestedLoops}). May cause O(n²) complexity",
                    context,
                    "Consider using dictionaries/maps for lookups, or extract to separate function"
                );
            }

            foreach (var violation in CheckNestedLoops(nestedLoop, newDepth, context))
            {
                yield return violation;
            }
        }
    }

    private static bool IsLoopNode(IUnifiedSyntaxNode node)
    {
        return node.Kind is UnifiedSyntaxKind.ForStatement or
               UnifiedSyntaxKind.ForEachStatement or
               UnifiedSyntaxKind.WhileStatement or
               UnifiedSyntaxKind.DoWhileStatement;
    }

    private static IUnifiedSyntaxNode? GetParentLoop(IUnifiedSyntaxNode node, IUnifiedSyntaxNode root)
    {
        var parent = node.Parent;
        while (parent != null && parent != root)
        {
            if (IsLoopNode(parent))
                return parent;
            parent = parent.Parent;
        }
        return parent == root ? root : null;
    }

    private static string GetLoopType(IUnifiedSyntaxNode loop)
    {
        return loop.Kind switch
        {
            UnifiedSyntaxKind.ForStatement => "for loop",
            UnifiedSyntaxKind.ForEachStatement => "foreach loop",
            UnifiedSyntaxKind.WhileStatement => "while loop",
            UnifiedSyntaxKind.DoWhileStatement => "do-while loop",
            _ => "loop"
        };
    }
}
