using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Async;

/// <summary>
/// Checks for ConfigureAwait usage in library code.
/// </summary>
[Rule("CS-ASYNC-003",
    Name = "ConfigureAwait Usage",
    Description = "Library code should use ConfigureAwait(false)",
    Severity = SeverityLevel.Low,
    Category = "async_best_practices")]
public class ConfigureAwaitAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("require_configure_await", Description = "Require ConfigureAwait on all awaits", DefaultValue = false)]
    public bool RequireConfigureAwait { get; set; } = false;

    [RuleParameter("prefer_false", Description = "Prefer ConfigureAwait(false)", DefaultValue = true)]
    public bool PreferFalse { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        if (!RequireConfigureAwait)
            yield break;

        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var awaitExpressions = root.DescendantNodes()
            .OfType<AwaitExpressionSyntax>();

        foreach (var awaitExpr in awaitExpressions)
        {
            // Check if ConfigureAwait is already called
            if (awaitExpr.Expression is InvocationExpressionSyntax invocation)
            {
                var methodName = GetMethodName(invocation);
                if (methodName == "ConfigureAwait")
                    continue;
            }

            // Skip if the awaited expression ends with ConfigureAwait
            if (awaitExpr.Expression.ToString().Contains("ConfigureAwait"))
                continue;

            yield return CreateViolation(
                awaitExpr.GetLocation(),
                "Consider using ConfigureAwait(false) in library code",
                context,
                "Add .ConfigureAwait(false) after the awaited expression"
            );
        }
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }
}
