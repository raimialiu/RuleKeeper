using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that checks for methods with high cyclomatic complexity.
/// Works with all supported languages through the unified AST.
/// </summary>
[Rule("XL-DESIGN-002",
    Name = "Cyclomatic Complexity (Cross-Language)",
    Description = "Methods should not have high cyclomatic complexity",
    Severity = SeverityLevel.Medium,
    Category = "design")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class CyclomaticComplexityCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    /// <summary>
    /// Maximum cyclomatic complexity allowed.
    /// </summary>
    [RuleParameter("max_complexity", Description = "Maximum cyclomatic complexity", DefaultValue = 10)]
    public int MaxComplexity { get; set; } = 10;

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var methods = context.GetMethods();

        foreach (var method in methods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var complexity = CalculateComplexity(method);

            if (complexity > MaxComplexity)
            {
                var methodName = GetMethodName(method);
                yield return CreateViolation(
                    method,
                    $"Method '{methodName}' has cyclomatic complexity of {complexity} (max: {MaxComplexity})",
                    context,
                    "Consider refactoring to reduce complexity"
                );
            }
        }
    }

    /// <summary>
    /// Calculates the cyclomatic complexity of a method.
    /// Complexity = 1 + number of decision points.
    /// </summary>
    private int CalculateComplexity(IUnifiedSyntaxNode method)
    {
        int complexity = 1; // Base complexity

        foreach (var descendant in method.Descendants())
        {
            // Count decision points
            switch (descendant.Kind)
            {
                // Conditional statements
                case UnifiedSyntaxKind.IfStatement:
                case UnifiedSyntaxKind.ElseClause:
                case UnifiedSyntaxKind.SwitchCase:
                case UnifiedSyntaxKind.ConditionalExpression: // Ternary operator
                    complexity++;
                    break;

                // Loops
                case UnifiedSyntaxKind.WhileStatement:
                case UnifiedSyntaxKind.DoWhileStatement:
                case UnifiedSyntaxKind.ForStatement:
                case UnifiedSyntaxKind.ForEachStatement:
                    complexity++;
                    break;

                // Exception handling
                case UnifiedSyntaxKind.CatchClause:
                    complexity++;
                    break;

                // Logical operators in binary expressions
                case UnifiedSyntaxKind.BinaryExpression:
                    var text = descendant.Text;
                    if (text.Contains("&&") || text.Contains("||") ||
                        text.Contains(" and ") || text.Contains(" or "))
                    {
                        complexity++;
                    }
                    break;

                // Null coalescing
                case UnifiedSyntaxKind.CoalesceExpression:
                    complexity++;
                    break;
            }
        }

        return complexity;
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
