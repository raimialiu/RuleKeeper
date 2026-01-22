using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures async methods end with 'Async' suffix.
/// </summary>
[Rule("CS-NAME-003",
    Name = "Async Method Naming Convention",
    Description = "Async methods must end with 'Async' suffix",
    Severity = SeverityLevel.Low,
    Category = "naming_conventions")]
public class AsyncNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("require_suffix", Description = "Require Async suffix on all async methods", DefaultValue = true)]
    public bool RequireSuffix { get; set; } = true;

    [RuleParameter("exclude_event_handlers", Description = "Exclude event handlers from check", DefaultValue = true)]
    public bool ExcludeEventHandlers { get; set; } = true;

    [RuleParameter("exclude_interface_implementations", Description = "Exclude interface implementations", DefaultValue = false)]
    public bool ExcludeInterfaceImplementations { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        if (!RequireSuffix)
            yield break;

        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var name = method.Identifier.Text;

            // Check if method is async
            var isAsync = method.Modifiers.Any(m => m.Text == "async");
            var returnType = method.ReturnType.ToString();
            var returnsTask = returnType.StartsWith("Task") ||
                             returnType.StartsWith("ValueTask") ||
                             returnType.Contains("Task<") ||
                             returnType.Contains("ValueTask<");

            if (!isAsync && !returnsTask)
                continue;

            // Skip if already has Async suffix
            if (name.EndsWith("Async"))
                continue;

            // Skip event handlers if configured
            if (ExcludeEventHandlers && IsEventHandler(method))
                continue;

            // Skip interface implementations if configured
            if (ExcludeInterfaceImplementations && IsExplicitInterfaceImplementation(method))
                continue;

            yield return CreateViolation(
                method.Identifier.GetLocation(),
                $"Async method '{name}' should end with 'Async' suffix",
                context,
                $"Rename the method to '{name}Async'"
            );
        }
    }

    private static bool IsEventHandler(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return false;

        var secondParam = parameters[1].Type?.ToString();
        return secondParam != null &&
               (secondParam.EndsWith("EventArgs") || secondParam == "EventArgs");
    }

    private static bool IsExplicitInterfaceImplementation(MethodDeclarationSyntax method)
    {
        return method.ExplicitInterfaceSpecifier != null;
    }
}
