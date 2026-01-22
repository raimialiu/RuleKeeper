using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Checks for methods that are too long.
/// </summary>
[Rule("CS-DESIGN-001",
    Name = "Method Length",
    Description = "Methods should not exceed a configurable number of lines",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class MethodLengthAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_lines", Description = "Maximum number of lines in a method", DefaultValue = 50)]
    public int MaxLines { get; set; } = 50;

    [RuleParameter("count_blank_lines", Description = "Include blank lines in count", DefaultValue = false)]
    public bool CountBlankLines { get; set; } = false;

    [RuleParameter("count_comments", Description = "Include comment lines in count", DefaultValue = false)]
    public bool CountComments { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var sourceText = context.SyntaxTree.GetText();

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            if (method.Body == null && method.ExpressionBody == null)
                continue;

            var methodSpan = method.Body?.Span ?? method.ExpressionBody!.Span;
            var startLine = sourceText.Lines.GetLineFromPosition(methodSpan.Start).LineNumber;
            var endLine = sourceText.Lines.GetLineFromPosition(methodSpan.End).LineNumber;

            var lineCount = CountLines(sourceText, startLine, endLine);

            if (lineCount > MaxLines)
            {
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{method.Identifier.Text}' has {lineCount} lines (max: {MaxLines})",
                    context,
                    "Consider breaking this method into smaller methods"
                );
            }
        }
    }

    private int CountLines(Microsoft.CodeAnalysis.Text.SourceText sourceText, int startLine, int endLine)
    {
        if (CountBlankLines && CountComments)
            return endLine - startLine + 1;

        var count = 0;
        for (var i = startLine; i <= endLine; i++)
        {
            var line = sourceText.Lines[i].ToString().Trim();

            if (!CountBlankLines && string.IsNullOrWhiteSpace(line))
                continue;

            if (!CountComments && (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*")))
                continue;

            count++;
        }

        return count;
    }
}
