using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects unreachable code (dead code) that will never execute.
/// This includes code after return, throw, break, or continue statements.
/// </summary>
[Rule("CS-CODE-002",
    Name = "Unreachable Code",
    Description = "Detects code that will never execute",
    Severity = SeverityLevel.Medium,
    Category = "code")]
public class UnreachableCodeAnalyzer : BaseRuleAnalyzer
{
    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var blocks = root.DescendantNodes().OfType<BlockSyntax>();

        foreach (var block in blocks)
        {
            var statements = block.Statements.ToList();

            for (int i = 0; i < statements.Count - 1; i++)
            {
                var statement = statements[i];

                if (IsTerminatingStatement(statement))
                {
                    // Check if there are statements after this one
                    for (int j = i + 1; j < statements.Count; j++)
                    {
                        var unreachable = statements[j];

                        // Skip labels (they can be jumped to)
                        if (unreachable is LabeledStatementSyntax)
                            continue;

                        yield return CreateViolation(
                            unreachable.GetLocation(),
                            "Unreachable code detected after terminating statement",
                            context,
                            "Remove the unreachable code or fix the control flow"
                        );
                    }
                    break; // No need to check further in this block
                }
            }
        }
    }

    private static bool IsTerminatingStatement(StatementSyntax statement)
    {
        return statement is ReturnStatementSyntax or
               ThrowStatementSyntax or
               BreakStatementSyntax or
               ContinueStatementSyntax or
               GotoStatementSyntax;
    }
}
