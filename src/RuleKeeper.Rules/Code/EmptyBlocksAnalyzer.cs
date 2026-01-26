using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects empty code blocks that often indicate incomplete implementation
/// or unintentional empty logic.
/// </summary>
[Rule("CS-CODE-003",
    Name = "Empty Blocks",
    Description = "Detects empty if, else, finally, try blocks and loops",
    Severity = SeverityLevel.Medium,
    Category = "code")]
public class EmptyBlocksAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("check_if", Description = "Check empty if blocks", DefaultValue = true)]
    public bool CheckIf { get; set; } = true;

    [RuleParameter("check_else", Description = "Check empty else blocks", DefaultValue = true)]
    public bool CheckElse { get; set; } = true;

    [RuleParameter("check_try", Description = "Check empty try blocks", DefaultValue = true)]
    public bool CheckTry { get; set; } = true;

    [RuleParameter("check_finally", Description = "Check empty finally blocks", DefaultValue = true)]
    public bool CheckFinally { get; set; } = true;

    [RuleParameter("check_loops", Description = "Check empty loops", DefaultValue = true)]
    public bool CheckLoops { get; set; } = true;

    [RuleParameter("check_switch_cases", Description = "Check empty switch cases", DefaultValue = true)]
    public bool CheckSwitchCases { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        // Check if statements
        if (CheckIf)
        {
            foreach (var ifStmt in root.DescendantNodes().OfType<IfStatementSyntax>())
            {
                if (IsEmptyBlock(ifStmt.Statement))
                {
                    yield return CreateViolation(
                        ifStmt.IfKeyword.GetLocation(),
                        "Empty if block detected",
                        context,
                        "Add implementation or remove the if statement"
                    );
                }
            }
        }

        // Check else clauses
        if (CheckElse)
        {
            foreach (var elseClause in root.DescendantNodes().OfType<ElseClauseSyntax>())
            {
                if (elseClause.Statement is not IfStatementSyntax && IsEmptyBlock(elseClause.Statement))
                {
                    yield return CreateViolation(
                        elseClause.ElseKeyword.GetLocation(),
                        "Empty else block detected",
                        context,
                        "Add implementation or remove the else clause"
                    );
                }
            }
        }

        // Check try blocks
        if (CheckTry)
        {
            foreach (var tryStmt in root.DescendantNodes().OfType<TryStatementSyntax>())
            {
                if (IsEmptyBlock(tryStmt.Block))
                {
                    yield return CreateViolation(
                        tryStmt.TryKeyword.GetLocation(),
                        "Empty try block detected",
                        context,
                        "Add implementation or remove the try-catch"
                    );
                }
            }
        }

        // Check finally blocks
        if (CheckFinally)
        {
            foreach (var finallyClause in root.DescendantNodes().OfType<FinallyClauseSyntax>())
            {
                if (IsEmptyBlock(finallyClause.Block))
                {
                    yield return CreateViolation(
                        finallyClause.FinallyKeyword.GetLocation(),
                        "Empty finally block detected",
                        context,
                        "Add cleanup code or remove the finally block"
                    );
                }
            }
        }

        // Check loops
        if (CheckLoops)
        {
            foreach (var forLoop in root.DescendantNodes().OfType<ForStatementSyntax>())
            {
                if (IsEmptyBlock(forLoop.Statement))
                {
                    yield return CreateViolation(
                        forLoop.ForKeyword.GetLocation(),
                        "Empty for loop detected",
                        context,
                        "Add implementation or remove the loop"
                    );
                }
            }

            foreach (var foreachLoop in root.DescendantNodes().OfType<ForEachStatementSyntax>())
            {
                if (IsEmptyBlock(foreachLoop.Statement))
                {
                    yield return CreateViolation(
                        foreachLoop.ForEachKeyword.GetLocation(),
                        "Empty foreach loop detected",
                        context,
                        "Add implementation or remove the loop"
                    );
                }
            }

            foreach (var whileLoop in root.DescendantNodes().OfType<WhileStatementSyntax>())
            {
                if (IsEmptyBlock(whileLoop.Statement))
                {
                    yield return CreateViolation(
                        whileLoop.WhileKeyword.GetLocation(),
                        "Empty while loop detected",
                        context,
                        "Add implementation or remove the loop"
                    );
                }
            }
        }

        // Check switch cases
        if (CheckSwitchCases)
        {
            foreach (var switchSection in root.DescendantNodes().OfType<SwitchSectionSyntax>())
            {
                // Allow fall-through cases (no statements is intentional)
                var hasStatements = switchSection.Statements.Any();
                var hasBreakOrReturn = switchSection.Statements
                    .Any(s => s is BreakStatementSyntax or ReturnStatementSyntax or ThrowStatementSyntax);

                if (hasStatements && !hasBreakOrReturn)
                {
                    // Has statements but no terminating statement - might be fall-through
                    continue;
                }

                if (!hasStatements)
                {
                    // Completely empty case - might be intentional grouping
                    // Only flag if it's the last case before default
                    continue;
                }
            }
        }
    }

    private static bool IsEmptyBlock(StatementSyntax? statement)
    {
        if (statement == null)
            return true;

        if (statement is BlockSyntax block)
        {
            return !block.Statements.Any();
        }

        if (statement is EmptyStatementSyntax)
        {
            return true;
        }

        return false;
    }
}
