using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Async;

/// <summary>
/// Detects async void methods (except event handlers).
/// </summary>
[Rule("CS-ASYNC-001",
    Name = "No Async Void",
    Description = "Async methods should return Task instead of void",
    Severity = SeverityLevel.High,
    Category = "async_best_practices")]
public class AsyncVoidAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_event_handlers", Description = "Allow async void for event handlers", DefaultValue = true)]
    public bool AllowEventHandlers { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var asyncMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.Text == "async"));

        foreach (var method in asyncMethods)
        {
            var returnType = method.ReturnType.ToString();

            if (returnType != "void")
                continue;

            // Check if it's an event handler
            if (AllowEventHandlers && IsEventHandler(method))
                continue;

            yield return CreateViolation(
                method.ReturnType.GetLocation(),
                $"Async method '{method.Identifier.Text}' should return Task instead of void",
                context,
                "Change return type from 'void' to 'Task'"
            );
        }
    }

    private static bool IsEventHandler(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return false;

        // Check for standard event handler signature
        var firstParam = parameters[0].Type?.ToString();
        var secondParam = parameters[1].Type?.ToString();

        return (firstParam == "object" || firstParam == "object?") &&
               (secondParam?.EndsWith("EventArgs") == true || secondParam == "EventArgs");
    }
}
