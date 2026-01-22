using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Checks for methods with too many parameters.
/// </summary>
[Rule("CS-DESIGN-002",
    Name = "Parameter Count",
    Description = "Methods should not have too many parameters",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class ParameterCountAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_parameters", Description = "Maximum number of parameters", DefaultValue = 5)]
    public int MaxParameters { get; set; } = 5;

    [RuleParameter("exclude_constructors", Description = "Exclude constructors from check", DefaultValue = false)]
    public bool ExcludeConstructors { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        // Check methods
        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var paramCount = method.ParameterList.Parameters.Count;
            if (paramCount > MaxParameters)
            {
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{method.Identifier.Text}' has {paramCount} parameters (max: {MaxParameters})",
                    context,
                    "Consider using a parameter object or builder pattern"
                );
            }
        }

        // Check constructors if not excluded
        if (!ExcludeConstructors)
        {
            var constructors = root.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>();

            foreach (var constructor in constructors)
            {
                var paramCount = constructor.ParameterList.Parameters.Count;
                if (paramCount > MaxParameters)
                {
                    yield return CreateViolation(
                        constructor.Identifier.GetLocation(),
                        $"Constructor has {paramCount} parameters (max: {MaxParameters})",
                        context,
                        "Consider using a builder pattern or breaking down the class"
                    );
                }
            }
        }
    }
}
