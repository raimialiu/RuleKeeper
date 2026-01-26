using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects methods with too many return statements.
/// Multiple return points can make code harder to follow and debug.
/// </summary>
[Rule("CS-CODE-005",
    Name = "Multiple Returns",
    Description = "Methods should have a limited number of return statements",
    Severity = SeverityLevel.Info,
    Category = "code")]
public class MultipleReturnsAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_returns", Description = "Maximum number of return statements", DefaultValue = 3)]
    public int MaxReturns { get; set; } = 3;

    [RuleParameter("allow_guard_clauses", Description = "Don't count early guard returns at method start", DefaultValue = true)]
    public bool AllowGuardClauses { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            if (method.Body == null)
                continue;

            var returnStatements = method.Body.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .ToList();

            var effectiveReturns = returnStatements;

            if (AllowGuardClauses)
            {
                effectiveReturns = returnStatements
                    .Where(r => !IsGuardClause(r, method.Body))
                    .ToList();
            }

            if (effectiveReturns.Count > MaxReturns)
            {
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{method.Identifier.Text}' has {effectiveReturns.Count} return statements (max: {MaxReturns})",
                    context,
                    "Consider extracting logic or using a single exit point pattern"
                );
            }
        }
    }

    private static bool IsGuardClause(ReturnStatementSyntax returnStmt, BlockSyntax methodBody)
    {
        // A guard clause is an early return at the beginning of a method
        // typically inside an if statement that checks preconditions

        var ifStatement = returnStmt.Ancestors()
            .OfType<IfStatementSyntax>()
            .FirstOrDefault();

        if (ifStatement == null)
            return false;

        // Check if this if statement is at the beginning of the method
        var statements = methodBody.Statements.ToList();
        var ifIndex = statements.IndexOf(ifStatement);

        // Consider it a guard clause if it's within the first few statements
        return ifIndex >= 0 && ifIndex < 3;
    }
}
