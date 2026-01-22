using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that checks for methods that are too long.
/// Works with all supported languages through the unified AST.
/// </summary>
[Rule("XL-DESIGN-001",
    Name = "Method Length (Cross-Language)",
    Description = "Methods should not exceed a configurable number of lines",
    Severity = SeverityLevel.Medium,
    Category = "design")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class MethodLengthCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    /// <summary>
    /// Maximum number of lines in a method.
    /// </summary>
    [RuleParameter("max_lines", Description = "Maximum number of lines in a method", DefaultValue = 50)]
    public int MaxLines { get; set; } = 50;

    /// <summary>
    /// Whether to count blank lines.
    /// </summary>
    [RuleParameter("count_blank_lines", Description = "Include blank lines in count", DefaultValue = false)]
    public bool CountBlankLines { get; set; } = false;

    /// <summary>
    /// Whether to count comment lines.
    /// </summary>
    [RuleParameter("count_comments", Description = "Include comment lines in count", DefaultValue = false)]
    public bool CountComments { get; set; } = false;

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var methods = context.GetMethods();

        foreach (var method in methods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var lineCount = context.GetLineCount(method, CountBlankLines, CountComments);

            if (lineCount > MaxLines)
            {
                var methodName = GetMethodName(method);
                yield return CreateViolation(
                    method,
                    $"Method '{methodName}' has {lineCount} lines (max: {MaxLines})",
                    context,
                    "Consider breaking this method into smaller methods"
                );
            }
        }
    }

    private string GetMethodName(IUnifiedSyntaxNode method)
    {
        // Try to extract the method name from the node
        var identifier = method.FirstChildOfKind(UnifiedSyntaxKind.Identifier);
        if (identifier != null)
            return identifier.Text;

        // Fallback to parsing the text
        var text = method.Text;
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            // Return first line truncated
            var firstLine = lines[0].Trim();
            if (firstLine.Length > 50)
                return firstLine.Substring(0, 47) + "...";
            return firstLine;
        }

        return "(unknown)";
    }
}
