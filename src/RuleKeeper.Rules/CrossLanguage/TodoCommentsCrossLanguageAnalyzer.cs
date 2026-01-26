using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that detects TODO, FIXME, HACK, and similar comments.
/// </summary>
[Rule("XL-CODE-001",
    Name = "TODO Comments (Cross-Language)",
    Description = "Detects TODO, FIXME, and similar comments that should be addressed",
    Severity = SeverityLevel.Info,
    Category = "code")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class TodoCommentsCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    [RuleParameter("patterns", Description = "Comma-separated patterns to detect", DefaultValue = "TODO,FIXME,HACK,XXX,BUG")]
    public string Patterns { get; set; } = "TODO,FIXME,HACK,XXX,BUG";

    [RuleParameter("case_sensitive", Description = "Use case-sensitive matching", DefaultValue = false)]
    public bool CaseSensitive { get; set; } = false;

    private List<string> _patternList = new();

    public override void Initialize(Dictionary<string, object> parameters)
    {
        base.Initialize(parameters);
        _patternList = Patterns
            .Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var lines = context.SourceText?.Split('\n') ?? Array.Empty<string>();
        var comparison = CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < lines.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];

            // Check if line contains a comment
            if (!IsCommentLine(line, context.Language))
                continue;

            foreach (var pattern in _patternList)
            {
                if (line.Contains(pattern, comparison))
                {
                    var truncated = line.Trim();
                    if (truncated.Length > 80)
                        truncated = truncated.Substring(0, 77) + "...";

                    var location = new SourceLocation(
                        context.FilePath,
                        i + 1, 1,
                        i + 1, line.Length);

                    yield return CreateViolation(
                        location,
                        $"{pattern} comment found: {truncated}",
                        context,
                        "Create a work item to track this and remove the comment"
                    );
                    break; // Only report once per line
                }
            }
        }
    }

    private static bool IsCommentLine(string line, Language language)
    {
        var trimmed = line.Trim();

        return language switch
        {
            Language.Python => trimmed.StartsWith("#"),
            _ => trimmed.StartsWith("//") ||
                 trimmed.StartsWith("/*") ||
                 trimmed.StartsWith("*") ||
                 trimmed.StartsWith("///")
        };
    }
}
