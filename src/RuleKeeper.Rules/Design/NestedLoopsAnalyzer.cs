using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects nested loops which often indicate O(n²) or worse algorithmic complexity.
/// Encourages use of LINQ, dictionaries, or method extraction instead.
/// </summary>
[Rule("CS-DESIGN-007",
    Name = "Nested Loops",
    Description = "Avoid nested loops which can cause performance issues",
    Severity = SeverityLevel.High,
    Category = "design")]
public class NestedLoopsAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_nested_loops", Description = "Maximum allowed nested loops (1 = no nesting)", DefaultValue = 1)]
    public int MaxNestedLoops { get; set; } = 1;

    [RuleParameter("allow_linq", Description = "Allow nested LINQ queries", DefaultValue = true)]
    public bool AllowLinq { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var topLevelLoops = GetTopLevelLoops(method);

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

    private static IEnumerable<StatementSyntax> GetTopLevelLoops(MethodDeclarationSyntax method)
    {
        if (method.Body == null)
            return Enumerable.Empty<StatementSyntax>();

        return method.Body.Statements
            .Where(IsLoopStatement)
            .Concat(method.Body.DescendantNodes()
                .OfType<StatementSyntax>()
                .Where(s => IsLoopStatement(s) && !IsNestedInLoop(s, method.Body)));
    }

    private static bool IsNestedInLoop(SyntaxNode node, BlockSyntax methodBody)
    {
        var parent = node.Parent;
        while (parent != null && parent != methodBody)
        {
            if (IsLoopStatement(parent))
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    private static bool IsLoopStatement(SyntaxNode node)
    {
        return node is ForStatementSyntax or
               ForEachStatementSyntax or
               WhileStatementSyntax or
               DoStatementSyntax;
    }

    private IEnumerable<Violation> CheckNestedLoops(SyntaxNode loop, int depth, AnalysisContext context)
    {
        var nestedLoops = loop.DescendantNodes()
            .Where(IsLoopStatement)
            .Where(n => n.Parent != null && GetParentLoop(n) == loop);

        foreach (var nestedLoop in nestedLoops)
        {
            var newDepth = depth + 1;

            if (newDepth > MaxNestedLoops)
            {
                var loopType = GetLoopType(nestedLoop);
                yield return CreateViolation(
                    nestedLoop.GetLocation(),
                    $"Nested {loopType} detected (depth: {newDepth}, max: {MaxNestedLoops}). This may cause O(n²) or worse complexity",
                    context,
                    "Consider using Dictionary/HashSet for lookups, LINQ methods, or extract to separate method"
                );
            }

            // Recursively check for deeper nesting
            foreach (var violation in CheckNestedLoops(nestedLoop, newDepth, context))
            {
                yield return violation;
            }
        }
    }

    private static SyntaxNode? GetParentLoop(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (IsLoopStatement(parent))
                return parent;
            parent = parent.Parent;
        }
        return null;
    }

    private static string GetLoopType(SyntaxNode loop)
    {
        return loop switch
        {
            ForStatementSyntax => "for loop",
            ForEachStatementSyntax => "foreach loop",
            WhileStatementSyntax => "while loop",
            DoStatementSyntax => "do-while loop",
            _ => "loop"
        };
    }
}
