using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Checks for methods with high cyclomatic complexity.
/// </summary>
[Rule("CS-DESIGN-003",
    Name = "Cyclomatic Complexity",
    Description = "Methods should not have high cyclomatic complexity",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class CyclomaticComplexityAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_complexity", Description = "Maximum cyclomatic complexity", DefaultValue = 10)]
    public int MaxComplexity { get; set; } = 10;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var complexity = CalculateComplexity(method);

            if (complexity > MaxComplexity)
            {
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{method.Identifier.Text}' has cyclomatic complexity of {complexity} (max: {MaxComplexity})",
                    context,
                    "Reduce complexity by extracting methods or simplifying conditionals"
                );
            }
        }
    }

    private static int CalculateComplexity(MethodDeclarationSyntax method)
    {
        // Start with 1 for the method itself
        var complexity = 1;

        foreach (var node in method.DescendantNodes())
        {
            complexity += node switch
            {
                IfStatementSyntax => 1,
                ElseClauseSyntax => 0, // Already counted with if
                ForStatementSyntax => 1,
                ForEachStatementSyntax => 1,
                WhileStatementSyntax => 1,
                DoStatementSyntax => 1,
                CaseSwitchLabelSyntax => 1,
                CasePatternSwitchLabelSyntax => 1,
                ConditionalExpressionSyntax => 1,
                CatchClauseSyntax => 1,
                ConditionalAccessExpressionSyntax => 1,
                BinaryExpressionSyntax binary when
                    binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                    binary.IsKind(SyntaxKind.LogicalOrExpression) ||
                    binary.IsKind(SyntaxKind.CoalesceExpression) => 1,
                _ => 0
            };
        }

        return complexity;
    }
}
