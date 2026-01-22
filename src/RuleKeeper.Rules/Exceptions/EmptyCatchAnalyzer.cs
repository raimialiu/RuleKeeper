using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Exceptions;

/// <summary>
/// Detects empty catch blocks.
/// </summary>
[Rule("CS-EXC-001",
    Name = "No Empty Catch Blocks",
    Description = "Catch blocks should not be empty",
    Severity = SeverityLevel.High,
    Category = "exceptions")]
public class EmptyCatchAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_comment", Description = "Allow empty catch with comment", DefaultValue = true)]
    public bool AllowComment { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var catchClauses = root.DescendantNodes()
            .OfType<CatchClauseSyntax>();

        foreach (var catchClause in catchClauses)
        {
            var block = catchClause.Block;
            if (block == null)
                continue;

            // Check if block is empty (no statements)
            var hasStatements = block.Statements.Any();

            if (!hasStatements)
            {
                // Check for comments if allowed
                if (AllowComment)
                {
                    var hasComment = block.GetLeadingTrivia()
                        .Concat(block.GetTrailingTrivia())
                        .Concat(block.OpenBraceToken.TrailingTrivia)
                        .Concat(block.CloseBraceToken.LeadingTrivia)
                        .Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                  t.IsKind(SyntaxKind.MultiLineCommentTrivia));

                    if (hasComment)
                        continue;
                }

                var exceptionType = catchClause.Declaration?.Type?.ToString() ?? "Exception";

                yield return CreateViolation(
                    catchClause.CatchKeyword.GetLocation(),
                    $"Empty catch block swallowing {exceptionType}",
                    context,
                    "Log the exception or handle it appropriately"
                );
            }
        }
    }
}
