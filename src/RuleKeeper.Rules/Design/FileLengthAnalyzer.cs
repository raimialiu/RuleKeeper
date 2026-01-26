using Microsoft.CodeAnalysis;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Checks for files that are too long, which often indicates
/// poor organization or too many responsibilities.
/// </summary>
[Rule("CS-DESIGN-010",
    Name = "File Length",
    Description = "Files should not exceed a configurable number of lines",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class FileLengthAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_lines", Description = "Maximum number of lines in a file", DefaultValue = 500)]
    public int MaxLines { get; set; } = 500;

    [RuleParameter("count_blank_lines", Description = "Include blank lines in count", DefaultValue = true)]
    public bool CountBlankLines { get; set; } = true;

    [RuleParameter("count_comments", Description = "Include comment lines in count", DefaultValue = true)]
    public bool CountComments { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var sourceText = context.SyntaxTree.GetText(context.CancellationToken);
        var totalLines = sourceText.Lines.Count;

        var effectiveLines = totalLines;

        if (!CountBlankLines || !CountComments)
        {
            effectiveLines = 0;
            foreach (var line in sourceText.Lines)
            {
                var text = line.ToString().Trim();

                if (!CountBlankLines && string.IsNullOrWhiteSpace(text))
                    continue;

                if (!CountComments && (text.StartsWith("//") || text.StartsWith("/*") || text.StartsWith("*")))
                    continue;

                effectiveLines++;
            }
        }

        if (effectiveLines > MaxLines)
        {
            var location = Location.Create(
                context.SyntaxTree,
                Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(0, 0));

            yield return CreateViolation(
                location,
                $"File has {effectiveLines} lines (max: {MaxLines}). Consider splitting into multiple files",
                context,
                "Split related functionality into separate files following Single Responsibility Principle"
            );
        }
    }
}
