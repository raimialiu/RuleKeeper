using Microsoft.CodeAnalysis;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects lines that are too long, which impacts readability
/// especially in code reviews and side-by-side diffs.
/// </summary>
[Rule("CS-CODE-004",
    Name = "Long Lines",
    Description = "Lines should not exceed a configurable length",
    Severity = SeverityLevel.Info,
    Category = "code")]
public class LongLinesAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_length", Description = "Maximum line length", DefaultValue = 120)]
    public int MaxLength { get; set; } = 120;

    [RuleParameter("ignore_urls", Description = "Ignore lines containing URLs", DefaultValue = true)]
    public bool IgnoreUrls { get; set; } = true;

    [RuleParameter("ignore_strings", Description = "Ignore lines that are mostly string literals", DefaultValue = true)]
    public bool IgnoreStrings { get; set; } = true;

    [RuleParameter("ignore_comments", Description = "Ignore comment lines", DefaultValue = false)]
    public bool IgnoreComments { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var sourceText = context.SyntaxTree.GetText(context.CancellationToken);

        for (int i = 0; i < sourceText.Lines.Count; i++)
        {
            var line = sourceText.Lines[i];
            var lineText = line.ToString();
            var length = lineText.Length;

            if (length <= MaxLength)
                continue;

            // Skip lines containing URLs
            if (IgnoreUrls && ContainsUrl(lineText))
                continue;

            // Skip lines that are mostly string literals
            if (IgnoreStrings && IsMostlyString(lineText))
                continue;

            // Skip comment lines
            if (IgnoreComments && IsCommentLine(lineText))
                continue;

            var location = Location.Create(
                context.SyntaxTree,
                line.Span);

            yield return CreateViolation(
                location,
                $"Line {i + 1} has {length} characters (max: {MaxLength})",
                context,
                "Break the line into multiple lines or extract to variables/methods"
            );
        }
    }

    private static bool ContainsUrl(string line)
    {
        return line.Contains("http://") ||
               line.Contains("https://") ||
               line.Contains("ftp://") ||
               line.Contains("file://");
    }

    private static bool IsMostlyString(string line)
    {
        var trimmed = line.Trim();
        var quoteCount = trimmed.Count(c => c == '"');
        return quoteCount >= 2 && trimmed.Contains("\"");
    }

    private static bool IsCommentLine(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("//") ||
               trimmed.StartsWith("/*") ||
               trimmed.StartsWith("*") ||
               trimmed.StartsWith("///");
    }
}
