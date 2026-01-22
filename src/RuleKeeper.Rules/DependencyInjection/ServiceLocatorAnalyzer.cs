using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.DependencyInjection;

/// <summary>
/// Detects use of service locator anti-pattern.
/// </summary>
[Rule("CS-DI-002",
    Name = "No Service Locator Pattern",
    Description = "Avoid using service locator pattern",
    Severity = SeverityLevel.Medium,
    Category = "dependency_injection")]
public class ServiceLocatorAnalyzer : BaseRuleAnalyzer
{
    private static readonly string[] ServiceLocatorMethods = new[]
    {
        "GetService", "GetRequiredService", "GetServices", "Resolve", "ResolveAll"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            if (methodName == null)
                continue;

            if (!ServiceLocatorMethods.Contains(methodName))
                continue;

            // Allow in certain contexts
            if (IsInAllowedContext(invocation))
                continue;

            yield return CreateViolation(
                invocation.GetLocation(),
                $"Avoid service locator pattern: {methodName}",
                context,
                "Use constructor injection instead of resolving services manually"
            );
        }
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ when invocation.Expression is MemberAccessExpressionSyntax ma &&
                   ma.Name is GenericNameSyntax gn => gn.Identifier.Text,
            _ => null
        };
    }

    private static bool IsInAllowedContext(InvocationExpressionSyntax invocation)
    {
        // Allow in extension methods
        var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null)
        {
            var methodName = containingMethod.Identifier.Text;

            // Allow in ConfigureServices, AddServices, etc.
            if (methodName.StartsWith("Configure") ||
                methodName.StartsWith("Add") ||
                methodName.StartsWith("Register") ||
                methodName == "Main" ||
                methodName == "CreateScope")
            {
                return true;
            }

            // Allow in factory methods
            if (methodName.StartsWith("Create") || methodName.EndsWith("Factory"))
            {
                return true;
            }
        }

        // Allow in Startup/Program classes
        var containingClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass != null)
        {
            var className = containingClass.Identifier.Text;
            if (className == "Startup" || className == "Program" ||
                className.EndsWith("ServiceCollectionExtensions"))
            {
                return true;
            }
        }

        return false;
    }
}
