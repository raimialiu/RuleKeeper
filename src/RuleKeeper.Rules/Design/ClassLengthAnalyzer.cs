using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Checks for classes that are too long.
/// </summary>
[Rule("CS-DESIGN-004",
    Name = "Class Length",
    Description = "Classes should not exceed a configurable number of lines",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class ClassLengthAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_lines", Description = "Maximum number of lines in a class", DefaultValue = 500)]
    public int MaxLines { get; set; } = 500;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var sourceText = context.SyntaxTree.GetText();

        var classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var cls in classes)
        {
            var startLine = sourceText.Lines.GetLineFromPosition(cls.SpanStart).LineNumber;
            var endLine = sourceText.Lines.GetLineFromPosition(cls.Span.End).LineNumber;
            var lineCount = endLine - startLine + 1;

            if (lineCount > MaxLines)
            {
                yield return CreateViolation(
                    cls.Identifier.GetLocation(),
                    $"Class '{cls.Identifier.Text}' has {lineCount} lines (max: {MaxLines})",
                    context,
                    "Consider breaking this class into smaller classes or extracting interfaces"
                );
            }
        }
    }
}
