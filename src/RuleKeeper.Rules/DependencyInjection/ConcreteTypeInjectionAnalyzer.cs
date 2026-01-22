using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.DependencyInjection;

/// <summary>
/// Detects injection of concrete types instead of interfaces.
/// </summary>
[Rule("CS-DI-001",
    Name = "Inject Interfaces Not Concrete Types",
    Description = "Constructor dependencies should be interfaces, not concrete types",
    Severity = SeverityLevel.Medium,
    Category = "dependency_injection")]
public class ConcreteTypeInjectionAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("exclude_primitives", Description = "Exclude primitive and common types", DefaultValue = true)]
    public bool ExcludePrimitives { get; set; } = true;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Primitives
        "string", "int", "long", "bool", "double", "float", "decimal",
        "byte", "short", "char", "object", "DateTime", "TimeSpan", "Guid",
        // Common framework types
        "ILogger", "IConfiguration", "IOptions", "IServiceProvider",
        "CancellationToken", "HttpClient", "IHttpClientFactory",
        // Collections
        "List", "Dictionary", "HashSet", "Array", "IEnumerable", "IList",
        "ICollection", "IDictionary", "IReadOnlyList", "IReadOnlyCollection"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var constructors = root.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>();

        foreach (var constructor in constructors)
        {
            // Skip private constructors
            if (constructor.Modifiers.Any(SyntaxKind.PrivateKeyword))
                continue;

            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                var typeName = parameter.Type?.ToString() ?? "";

                // Skip interfaces (starts with I followed by uppercase)
                if (IsInterface(typeName))
                    continue;

                // Skip allowed types
                if (ExcludePrimitives && IsAllowedType(typeName))
                    continue;

                // Skip generic type parameters
                if (typeName.Length == 1 && char.IsUpper(typeName[0]))
                    continue;

                // Check if it looks like a concrete class
                if (IsLikelyConcreteClass(typeName))
                {
                    yield return CreateViolation(
                        parameter.Type?.GetLocation() ?? parameter.GetLocation(),
                        $"Consider injecting an interface instead of concrete type '{typeName}'",
                        context,
                        $"Create an interface I{typeName} and inject that instead"
                    );
                }
            }
        }
    }

    private static bool IsInterface(string typeName)
    {
        // Extract base type name without generic parameters
        var baseName = typeName.Split('<')[0].Trim();

        // Standard .NET interface naming: starts with 'I' followed by uppercase
        return baseName.Length > 1 &&
               baseName.StartsWith("I") &&
               char.IsUpper(baseName[1]);
    }

    private static bool IsAllowedType(string typeName)
    {
        var baseName = typeName.Split('<')[0].Trim();
        var simpleName = baseName.Split('.').Last();

        return AllowedTypes.Contains(simpleName) ||
               typeName.EndsWith("?") && AllowedTypes.Contains(typeName.TrimEnd('?'));
    }

    private static bool IsLikelyConcreteClass(string typeName)
    {
        var baseName = typeName.Split('<')[0].Trim();
        var simpleName = baseName.Split('.').Last();

        // Common service/repository naming patterns
        return simpleName.EndsWith("Service") ||
               simpleName.EndsWith("Repository") ||
               simpleName.EndsWith("Manager") ||
               simpleName.EndsWith("Provider") ||
               simpleName.EndsWith("Handler") ||
               simpleName.EndsWith("Factory") ||
               simpleName.EndsWith("Client") ||
               simpleName.EndsWith("Context");
    }
}
