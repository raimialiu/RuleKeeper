using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Async;

/// <summary>
/// Detects blocking calls on async operations (.Result, .Wait()).
/// </summary>
[Rule("CS-ASYNC-002",
    Name = "No Blocking on Async",
    Description = "Avoid .Result and .Wait() on Tasks which can cause deadlocks",
    Severity = SeverityLevel.High,
    Category = "async_best_practices")]
public class BlockingAsyncAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_in_main", Description = "Allow blocking in Main method", DefaultValue = false)]
    public bool AllowInMain { get; set; } = false;

    [RuleParameter("allow_in_tests", Description = "Allow blocking in test methods", DefaultValue = false)]
    public bool AllowInTests { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        // Check for .Result access
        var memberAccesses = root.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Name.Identifier.Text == "Result");

        foreach (var access in memberAccesses)
        {
            if (ShouldSkip(access))
                continue;

            // Check if accessing Task.Result
            if (IsTaskType(access, context))
            {
                yield return CreateViolation(
                    access.Name.GetLocation(),
                    "Avoid blocking on async with .Result - use 'await' instead",
                    context,
                    "Replace .Result with await"
                );
            }
        }

        // Check for .Wait() calls
        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                if (methodName == "Wait" || methodName == "WaitAll" || methodName == "WaitAny")
                {
                    if (ShouldSkip(invocation))
                        continue;

                    if (IsTaskType(memberAccess, context) ||
                        memberAccess.Expression.ToString().StartsWith("Task"))
                    {
                        yield return CreateViolation(
                            memberAccess.Name.GetLocation(),
                            $"Avoid blocking on async with .{methodName}() - use async/await pattern",
                            context,
                            $"Replace .{methodName}() with await (or await Task.WhenAll/WhenAny)"
                        );
                    }
                }
            }
        }

        // Check for .GetAwaiter().GetResult()
        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "GetResult")
            {
                if (memberAccess.Expression is InvocationExpressionSyntax innerInvocation &&
                    innerInvocation.Expression is MemberAccessExpressionSyntax innerAccess &&
                    innerAccess.Name.Identifier.Text == "GetAwaiter")
                {
                    if (ShouldSkip(invocation))
                        continue;

                    yield return CreateViolation(
                        invocation.GetLocation(),
                        "Avoid blocking on async with .GetAwaiter().GetResult() - use 'await' instead",
                        context,
                        "Replace .GetAwaiter().GetResult() with await"
                    );
                }
            }
        }
    }

    private bool ShouldSkip(SyntaxNode node)
    {
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method == null)
            return false;

        // Skip Main method if configured
        if (AllowInMain && method.Identifier.Text == "Main")
            return true;

        // Skip test methods if configured
        if (AllowInTests)
        {
            var attributes = method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => a.Name.ToString());

            if (attributes.Any(a => a.Contains("Test") || a.Contains("Fact") || a.Contains("Theory")))
                return true;
        }

        return false;
    }

    private static bool IsTaskType(MemberAccessExpressionSyntax memberAccess, AnalysisContext context)
    {
        if (context.SemanticModel == null)
        {
            // Without semantic model, use heuristics
            var expressionText = memberAccess.Expression.ToString();
            return expressionText.Contains("Task") ||
                   expressionText.Contains("Async") ||
                   expressionText.EndsWith("Async()");
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
        var typeName = typeInfo.Type?.ToString() ?? "";

        return typeName.StartsWith("System.Threading.Tasks.Task") ||
               typeName.StartsWith("System.Threading.Tasks.ValueTask");
    }
}
