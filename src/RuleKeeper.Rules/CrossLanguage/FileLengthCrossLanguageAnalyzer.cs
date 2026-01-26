using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that checks for files that are too long.
/// </summary>
[Rule("XL-DESIGN-006",
    Name = "File Length (Cross-Language)",
    Description = "Files should not exceed a configurable number of lines",
    Severity = SeverityLevel.Medium,
    Category = "design")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class FileLengthCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    [RuleParameter("max_lines", Description = "Maximum number of lines in a file", DefaultValue = 500)]
    public int MaxLines { get; set; } = 500;

    [RuleParameter("count_blank_lines", Description = "Include blank lines in count", DefaultValue = true)]
    public bool CountBlankLines { get; set; } = true;

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var lines = context.SourceText?.Split('\n') ?? Array.Empty<string>();
        var totalLines = lines.Length;

        var effectiveLines = CountBlankLines
            ? totalLines
            : lines.Count(l => !string.IsNullOrWhiteSpace(l));

        if (effectiveLines > MaxLines)
        {
            var location = new SourceLocation(
                context.FilePath,
                1, 1,
                1, 1);

            yield return CreateViolation(
                location,
                $"File has {effectiveLines} lines (max: {MaxLines}). Consider splitting into multiple files",
                context,
                "Split related functionality into separate files"
            );
        }
    }
}
