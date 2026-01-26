using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects TODO, FIXME, HACK, and similar comments that indicate
/// incomplete or problematic code.
/// </summary>
[Rule("CS-CODE-001",
    Name = "TODO Comments",
    Description = "Detects TODO, FIXME, and similar comments that should be addressed",
    Severity = SeverityLevel.Info,
    Category = "code")]
public class TodoCommentsAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("patterns", Description = "Comma-separated patterns to detect", DefaultValue = "TODO,FIXME,HACK,XXX,BUG,REFACTOR")]
    public string Patterns { get; set; } = "TODO,FIXME,HACK,XXX,BUG,REFACTOR";

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

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var triviaList = root.DescendantTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                        t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                        t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

        foreach (var trivia in triviaList)
        {
            var commentText = trivia.ToString();
            var comparison = CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            foreach (var pattern in _patternList)
            {
                if (commentText.Contains(pattern, comparison))
                {
                    var truncated = commentText.Length > 80
                        ? commentText.Substring(0, 77) + "..."
                        : commentText;

                    yield return CreateViolation(
                        trivia.GetLocation(),
                        $"{pattern} comment found: {truncated.Trim()}",
                        context,
                        "Create a work item to track this and remove the comment"
                    );
                    break; // Only report once per comment
                }
            }
        }
    }
}
