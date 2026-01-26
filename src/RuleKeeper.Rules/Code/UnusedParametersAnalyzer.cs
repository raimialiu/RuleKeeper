using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects method parameters that are never used within the method body.
/// Unused parameters may indicate incomplete implementation or API changes.
/// </summary>
[Rule("CS-CODE-007",
    Name = "Unused Parameters",
    Description = "Detects method parameters that are never used",
    Severity = SeverityLevel.Medium,
    Category = "code")]
public class UnusedParametersAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("ignore_underscore", Description = "Ignore parameters starting with underscore", DefaultValue = true)]
    public bool IgnoreUnderscore { get; set; } = true;

    [RuleParameter("ignore_overrides", Description = "Ignore parameters in override methods", DefaultValue = true)]
    public bool IgnoreOverrides { get; set; } = true;

    [RuleParameter("ignore_interface_impl", Description = "Ignore parameters in interface implementations", DefaultValue = true)]
    public bool IgnoreInterfaceImpl { get; set; } = true;

    [RuleParameter("ignore_event_handlers", Description = "Ignore event handler parameters (sender, e)", DefaultValue = true)]
    public bool IgnoreEventHandlers { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var semanticModel = context.SemanticModel;

        if (semanticModel == null)
            yield break;

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Skip abstract methods
            if (method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                continue;

            // Skip methods without body
            if (method.Body == null && method.ExpressionBody == null)
                continue;

            // Skip overrides if configured
            if (IgnoreOverrides && method.Modifiers.Any(SyntaxKind.OverrideKeyword))
                continue;

            // Check for interface implementation
            if (IgnoreInterfaceImpl)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol != null && IsInterfaceImplementation(methodSymbol))
                    continue;
            }

            // Check for event handler pattern
            if (IgnoreEventHandlers && IsEventHandler(method))
                continue;

            foreach (var parameter in method.ParameterList.Parameters)
            {
                var paramName = parameter.Identifier.Text;

                // Skip discard parameters
                if (paramName == "_")
                    continue;

                // Skip underscore-prefixed if configured
                if (IgnoreUnderscore && paramName.StartsWith("_"))
                    continue;

                // Find usages of this parameter
                var bodyNode = (SyntaxNode?)method.Body ?? method.ExpressionBody;
                if (bodyNode == null)
                    continue;

                var usages = bodyNode.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.Text == paramName)
                    .ToList();

                if (!usages.Any())
                {
                    yield return CreateViolation(
                        parameter.GetLocation(),
                        $"Parameter '{paramName}' is never used in method '{method.Identifier.Text}'",
                        context,
                        "Remove the unused parameter or prefix with underscore to indicate intentionally unused"
                    );
                }
            }
        }
    }

    private static bool IsInterfaceImplementation(IMethodSymbol methodSymbol)
    {
        return methodSymbol.ContainingType.AllInterfaces
            .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .Any(m => methodSymbol.Equals(
                methodSymbol.ContainingType.FindImplementationForInterfaceMember(m),
                SymbolEqualityComparer.Default));
    }

    private static bool IsEventHandler(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return false;

        var firstParam = parameters[0].Type?.ToString().ToLowerInvariant();
        var secondParam = parameters[1].Type?.ToString().ToLowerInvariant();

        return (firstParam == "object" || firstParam?.Contains("sender") == true) &&
               (secondParam?.Contains("eventargs") == true || secondParam?.EndsWith("args") == true);
    }
}
