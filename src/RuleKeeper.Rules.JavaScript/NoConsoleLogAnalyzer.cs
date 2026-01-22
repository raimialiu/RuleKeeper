using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.JavaScript;

/// <summary>
/// JavaScript/TypeScript rule that detects console.log statements.
/// </summary>
[Rule("JS-DEBUG-001",
    Name = "No Console Log",
    Description = "console.log statements should not be committed to production code",
    Severity = SeverityLevel.Low,
    Category = "debugging")]
[SupportedLanguages(Language.JavaScript, Language.TypeScript)]
public class NoConsoleLogAnalyzer : BaseCrossLanguageRule, ILanguageSpecificRule
{
    /// <inheritdoc />
    public Language TargetLanguage => Language.JavaScript;

    /// <summary>
    /// Whether to allow console.error and console.warn.
    /// </summary>
    [RuleParameter("allow_error_warn", Description = "Allow console.error and console.warn", DefaultValue = true)]
    public bool AllowErrorWarn { get; set; } = true;

    /// <summary>
    /// Patterns to exclude from checking (e.g., test files).
    /// </summary>
    [RuleParameter("exclude_patterns", Description = "File patterns to exclude", DefaultValue = "*.test.js,*.spec.js,*.test.ts,*.spec.ts")]
    public string ExcludePatterns { get; set; } = "*.test.js,*.spec.js,*.test.ts,*.spec.ts";

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var filePath = context.FilePath;

        // Check if file should be excluded
        var excludeList = ExcludePatterns.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pattern in excludeList)
        {
            var trimmedPattern = pattern.Trim();
            if (MatchesPattern(filePath, trimmedPattern))
                yield break;
        }

        var lines = context.Root.Text.Split('\n');
        int lineNumber = 1;

        foreach (var line in lines)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var trimmed = line.Trim();

            // Skip comments
            if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            {
                lineNumber++;
                continue;
            }

            // Check for console statements
            if (trimmed.Contains("console."))
            {
                var consoleMethod = ExtractConsoleMethod(trimmed);

                // Skip allowed methods
                if (AllowErrorWarn && (consoleMethod == "error" || consoleMethod == "warn"))
                {
                    lineNumber++;
                    continue;
                }

                if (!string.IsNullOrEmpty(consoleMethod))
                {
                    var location = new SourceLocation(filePath, lineNumber, 1, lineNumber, line.Length);
                    yield return Violation.FromSourceLocation(
                        location,
                        RuleId,
                        RuleName,
                        $"Remove console.{consoleMethod}() statement",
                        DefaultSeverity,
                        context.Language,
                        "Use a proper logging framework instead"
                    );
                }
            }

            lineNumber++;
        }
    }

    private static string ExtractConsoleMethod(string line)
    {
        var consoleIndex = line.IndexOf("console.");
        if (consoleIndex < 0)
            return "";

        var afterConsole = line.Substring(consoleIndex + 8);
        var parenIndex = afterConsole.IndexOf('(');
        if (parenIndex > 0)
        {
            return afterConsole.Substring(0, parenIndex).Trim();
        }

        return "";
    }

    private static bool MatchesPattern(string filePath, string pattern)
    {
        // Simple pattern matching for common cases
        if (pattern.StartsWith("*"))
        {
            var suffix = pattern.Substring(1);
            return filePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
