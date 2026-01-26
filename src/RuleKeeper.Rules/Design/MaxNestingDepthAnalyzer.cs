using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Checks for code that has too many levels of nesting.
/// Deep nesting makes code harder to read and maintain.
/// </summary>
[Rule("CS-DESIGN-005",
    Name = "Max Nesting Depth",
    Description = "Code should not have too many levels of nesting",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class MaxNestingDepthAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_depth", Description = "Maximum nesting depth allowed", DefaultValue = 3)]
    public int MaxDepth { get; set; } = 3;

    [RuleParameter("count_try_catch", Description = "Count try-catch as nesting level", DefaultValue = true)]
    public bool CountTryCatch { get; set; } = true;

    [RuleParameter("count_switch", Description = "Count switch statements as nesting level", DefaultValue = true)]
    public bool CountSwitch { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var violations = CheckNestingDepth(method, 0, context);
            foreach (var violation in violations)
            {
                yield return violation;
            }
        }
    }

    private IEnumerable<Violation> CheckNestingDepth(SyntaxNode node, int currentDepth, AnalysisContext context)
    {
        foreach (var child in node.ChildNodes())
        {
            var newDepth = currentDepth;
            var isNestingNode = false;

            if (child is IfStatementSyntax or
                ForStatementSyntax or
                ForEachStatementSyntax or
                WhileStatementSyntax or
                DoStatementSyntax)
            {
                isNestingNode = true;
                newDepth++;
            }
            else if (CountSwitch && child is SwitchStatementSyntax)
            {
                isNestingNode = true;
                newDepth++;
            }
            else if (CountTryCatch && child is TryStatementSyntax)
            {
                isNestingNode = true;
                newDepth++;
            }

            if (newDepth > MaxDepth && isNestingNode)
            {
                yield return CreateViolation(
                    child.GetLocation(),
                    $"Nesting depth of {newDepth} exceeds maximum of {MaxDepth}",
                    context,
                    "Consider extracting nested code into separate methods or using early returns"
                );
            }

            // Recursively check children
            foreach (var violation in CheckNestingDepth(child, newDepth, context))
            {
                yield return violation;
            }
        }
    }
}
