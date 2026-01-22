using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Exceptions;

/// <summary>
/// Detects throwing exceptions in finally blocks.
/// </summary>
[Rule("CS-EXC-003",
    Name = "No Throw in Finally",
    Description = "Avoid throwing exceptions in finally blocks",
    Severity = SeverityLevel.High,
    Category = "exceptions")]
public class ThrowingInFinallyAnalyzer : BaseRuleAnalyzer
{
    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var finallyBlocks = root.DescendantNodes()
            .OfType<FinallyClauseSyntax>();

        foreach (var finallyClause in finallyBlocks)
        {
            var throwStatements = finallyClause.DescendantNodes()
                .OfType<ThrowStatementSyntax>();

            foreach (var throwStmt in throwStatements)
            {
                yield return CreateViolation(
                    throwStmt.ThrowKeyword.GetLocation(),
                    "Avoid throwing exceptions in finally blocks",
                    context,
                    "Move exception throwing outside the finally block or handle it differently"
                );
            }
        }
    }
}
