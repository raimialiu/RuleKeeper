using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that detects lines that are too long.
/// </summary>
[Rule("XL-CODE-002",
    Name = "Long Lines (Cross-Language)",
    Description = "Lines should not exceed a configurable length",
    Severity = SeverityLevel.Info,
    Category = "code")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class LongLinesCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    [RuleParameter("max_length", Description = "Maximum line length", DefaultValue = 120)]
    public int MaxLength { get; set; } = 120;

    [RuleParameter("ignore_urls", Description = "Ignore lines containing URLs", DefaultValue = true)]
    public bool IgnoreUrls { get; set; } = true;

    [RuleParameter("ignore_imports", Description = "Ignore import/using statements", DefaultValue = true)]
    public bool IgnoreImports { get; set; } = true;

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var lines = context.SourceText?.Split('\n') ?? Array.Empty<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];
            var length = line.TrimEnd().Length;

            if (length <= MaxLength)
                continue;

            // Skip lines containing URLs
            if (IgnoreUrls && ContainsUrl(line))
                continue;

            // Skip import statements
            if (IgnoreImports && IsImportStatement(line, context.Language))
                continue;

            var location = new SourceLocation(
                context.FilePath,
                i + 1, 1,
                i + 1, length);

            yield return CreateViolation(
                location,
                $"Line {i + 1} has {length} characters (max: {MaxLength})",
                context,
                "Break the line into multiple lines"
            );
        }
    }

    private static bool ContainsUrl(string line)
    {
        return line.Contains("http://") ||
               line.Contains("https://") ||
               line.Contains("ftp://");
    }

    private static bool IsImportStatement(string line, Language language)
    {
        var trimmed = line.Trim();

        return language switch
        {
            Language.Python => trimmed.StartsWith("import ") || trimmed.StartsWith("from "),
            Language.Java => trimmed.StartsWith("import "),
            Language.Go => trimmed.StartsWith("import "),
            Language.CSharp => trimmed.StartsWith("using "),
            Language.JavaScript or Language.TypeScript =>
                trimmed.StartsWith("import ") || trimmed.StartsWith("require("),
            _ => false
        };
    }
}
