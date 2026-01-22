using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.JavaScript;

/// <summary>
/// JavaScript/TypeScript rule that prefers const/let over var.
/// </summary>
[Rule("JS-VAR-001",
    Name = "Prefer Const/Let",
    Description = "Use const or let instead of var for variable declarations",
    Severity = SeverityLevel.Medium,
    Category = "best-practices")]
[SupportedLanguages(Language.JavaScript, Language.TypeScript)]
public class PreferConstLetAnalyzer : BaseCrossLanguageRule, ILanguageSpecificRule
{
    /// <inheritdoc />
    public Language TargetLanguage => Language.JavaScript;

    /// <summary>
    /// Whether to suggest const as the preferred alternative.
    /// </summary>
    [RuleParameter("prefer_const", Description = "Suggest const when possible", DefaultValue = true)]
    public bool PreferConst { get; set; } = true;

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var lines = context.Root.Text.Split('\n');
        int lineNumber = 1;
        bool inMultilineComment = false;

        foreach (var line in lines)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var trimmed = line.Trim();

            // Track multiline comments
            if (trimmed.Contains("/*"))
                inMultilineComment = true;
            if (trimmed.Contains("*/"))
            {
                inMultilineComment = false;
                lineNumber++;
                continue;
            }

            if (inMultilineComment)
            {
                lineNumber++;
                continue;
            }

            // Skip single-line comments
            if (trimmed.StartsWith("//"))
            {
                lineNumber++;
                continue;
            }

            // Check for var declarations
            if (HasVarDeclaration(trimmed))
            {
                var suggestion = PreferConst ? "const" : "let";
                var location = new SourceLocation(context.FilePath, lineNumber, 1, lineNumber, line.Length);

                yield return Violation.FromSourceLocation(
                    location,
                    RuleId,
                    RuleName,
                    $"Use '{suggestion}' instead of 'var' for variable declaration",
                    DefaultSeverity,
                    context.Language,
                    $"Replace 'var' with '{suggestion}' (or 'let' if reassignment is needed)"
                );
            }

            lineNumber++;
        }
    }

    private static bool HasVarDeclaration(string line)
    {
        // Check for var keyword as a declaration (not inside string/comment)
        var trimmed = line.Trim();

        // Simple pattern matching for var declarations
        if (trimmed.StartsWith("var ") ||
            trimmed.Contains(" var ") ||
            trimmed.Contains("(var ") ||
            trimmed.Contains(",var ") ||
            trimmed.Contains(";var "))
        {
            // Make sure it's not in a string
            var varIndex = trimmed.IndexOf("var ");
            if (varIndex >= 0)
            {
                var beforeVar = trimmed.Substring(0, varIndex);
                // Check if we're inside a string (basic check)
                var quoteCount = beforeVar.Count(c => c == '"' || c == '\'' || c == '`');
                return quoteCount % 2 == 0; // Even number means we're not in a string
            }
        }

        return false;
    }
}
