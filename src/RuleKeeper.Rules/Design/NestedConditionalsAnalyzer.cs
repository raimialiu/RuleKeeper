using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects nested if statements that make code harder to read.
/// Enforces the "no deeply nested conditionals" coding standard.
/// </summary>
[Rule("CS-DESIGN-006",
    Name = "Nested Conditionals",
    Description = "Avoid deeply nested if statements",
    Severity = SeverityLevel.High,
    Category = "design")]
public class NestedConditionalsAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_nested_ifs", Description = "Maximum allowed nested if statements", DefaultValue = 2)]
    public int MaxNestedIfs { get; set; } = 2;

    [RuleParameter("count_else_if", Description = "Count else-if as nested", DefaultValue = false)]
    public bool CountElseIf { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var topLevelIfs = method.DescendantNodes()
                .OfType<IfStatementSyntax>()
                .Where(i => !(i.Parent is IfStatementSyntax) && !(i.Parent is ElseClauseSyntax));

            foreach (var ifStatement in topLevelIfs)
            {
                var violations = CheckNestedIfs(ifStatement, 1, context);
                foreach (var violation in violations)
                {
                    yield return violation;
                }
            }
        }
    }

    private IEnumerable<Violation> CheckNestedIfs(IfStatementSyntax ifStatement, int depth, AnalysisContext context)
    {
        // Check the 'then' branch
        if (ifStatement.Statement != null)
        {
            var nestedIfs = GetDirectNestedIfs(ifStatement.Statement);
            foreach (var nestedIf in nestedIfs)
            {
                var newDepth = depth + 1;
                if (newDepth > MaxNestedIfs)
                {
                    yield return CreateViolation(
                        nestedIf.GetLocation(),
                        $"Nested if depth of {newDepth} exceeds maximum of {MaxNestedIfs}",
                        context,
                        "Use guard clauses (early returns), extract methods, or combine conditions"
                    );
                }

                // Recursively check deeper nesting
                foreach (var violation in CheckNestedIfs(nestedIf, newDepth, context))
                {
                    yield return violation;
                }
            }
        }

        // Check the 'else' branch
        if (ifStatement.Else != null)
        {
            if (ifStatement.Else.Statement is IfStatementSyntax elseIf)
            {
                // else if - only count as nesting if configured
                var elseIfDepth = CountElseIf ? depth + 1 : depth;
                foreach (var violation in CheckNestedIfs(elseIf, elseIfDepth, context))
                {
                    yield return violation;
                }
            }
            else if (ifStatement.Else.Statement != null)
            {
                // else block - check for nested ifs
                var nestedIfs = GetDirectNestedIfs(ifStatement.Else.Statement);
                foreach (var nestedIf in nestedIfs)
                {
                    var newDepth = depth + 1;
                    if (newDepth > MaxNestedIfs)
                    {
                        yield return CreateViolation(
                            nestedIf.GetLocation(),
                            $"Nested if depth of {newDepth} exceeds maximum of {MaxNestedIfs}",
                            context,
                            "Use guard clauses (early returns), extract methods, or combine conditions"
                        );
                    }

                    foreach (var violation in CheckNestedIfs(nestedIf, newDepth, context))
                    {
                        yield return violation;
                    }
                }
            }
        }
    }

    private static IEnumerable<IfStatementSyntax> GetDirectNestedIfs(StatementSyntax statement)
    {
        if (statement is BlockSyntax block)
        {
            return block.Statements.OfType<IfStatementSyntax>();
        }
        else if (statement is IfStatementSyntax ifStmt)
        {
            return new[] { ifStmt };
        }
        return Enumerable.Empty<IfStatementSyntax>();
    }
}
