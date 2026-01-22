using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that checks for methods with too many parameters.
/// Works with all supported languages through the unified AST.
/// </summary>
[Rule("XL-DESIGN-003",
    Name = "Parameter Count (Cross-Language)",
    Description = "Methods should not have too many parameters",
    Severity = SeverityLevel.Medium,
    Category = "design")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class ParameterCountCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    /// <summary>
    /// Maximum number of parameters allowed.
    /// </summary>
    [RuleParameter("max_parameters", Description = "Maximum number of parameters", DefaultValue = 5)]
    public int MaxParameters { get; set; } = 5;

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var methods = context.GetMethods();

        foreach (var method in methods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var parameterCount = CountParameters(method);

            if (parameterCount > MaxParameters)
            {
                var methodName = GetMethodName(method);
                yield return CreateViolation(
                    method,
                    $"Method '{methodName}' has {parameterCount} parameters (max: {MaxParameters})",
                    context,
                    "Consider using a parameter object or breaking down the method"
                );
            }
        }
    }

    private int CountParameters(IUnifiedSyntaxNode method)
    {
        // Look for parameter list
        var parameterList = method.FirstChildOfKind(UnifiedSyntaxKind.ParameterList);
        if (parameterList != null)
        {
            return parameterList.Children.Count(c => c.Kind == UnifiedSyntaxKind.Parameter);
        }

        // Count parameters directly
        return method.DescendantsOfKind(UnifiedSyntaxKind.Parameter).Count();
    }

    private string GetMethodName(IUnifiedSyntaxNode method)
    {
        var identifier = method.FirstChildOfKind(UnifiedSyntaxKind.Identifier);
        if (identifier != null)
            return identifier.Text;

        var text = method.Text;
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            var firstLine = lines[0].Trim();
            if (firstLine.Length > 50)
                return firstLine.Substring(0, 47) + "...";
            return firstLine;
        }

        return "(unknown)";
    }
}
