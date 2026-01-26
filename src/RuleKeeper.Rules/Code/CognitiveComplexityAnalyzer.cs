using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Measures cognitive complexity - how difficult code is to understand.
/// Unlike cyclomatic complexity, this accounts for nesting and mental effort.
/// </summary>
[Rule("CS-CODE-008",
    Name = "Cognitive Complexity",
    Description = "Methods should not be too cognitively complex",
    Severity = SeverityLevel.Medium,
    Category = "code")]
public class CognitiveComplexityAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("max_complexity", Description = "Maximum cognitive complexity", DefaultValue = 15)]
    public int MaxComplexity { get; set; } = 15;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var complexity = CalculateCognitiveComplexity(method);

            if (complexity > MaxComplexity)
            {
                yield return CreateViolation(
                    method.Identifier.GetLocation(),
                    $"Method '{method.Identifier.Text}' has cognitive complexity of {complexity} (max: {MaxComplexity})",
                    context,
                    "Reduce complexity by extracting methods, reducing nesting, or simplifying logic"
                );
            }
        }
    }

    private static int CalculateCognitiveComplexity(MethodDeclarationSyntax method)
    {
        return CalculateComplexity(method, 0);
    }

    private static int CalculateComplexity(SyntaxNode node, int nestingLevel)
    {
        var complexity = 0;

        foreach (var child in node.ChildNodes())
        {
            complexity += child switch
            {
                // Control flow structures add 1 + nesting level
                IfStatementSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),
                ForStatementSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),
                ForEachStatementSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),
                WhileStatementSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),
                DoStatementSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),
                SwitchStatementSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),
                CatchClauseSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),

                // Else adds 1 (but else-if doesn't increase nesting)
                ElseClauseSyntax elseClause when elseClause.Statement is not IfStatementSyntax
                    => 1 + CalculateComplexity(child, nestingLevel + 1),
                ElseClauseSyntax elseClause when elseClause.Statement is IfStatementSyntax
                    => 1 + CalculateComplexity(child, nestingLevel),

                // Logical operators add 1 each for breaks in linear flow
                BinaryExpressionSyntax binary when
                    binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                    binary.IsKind(SyntaxKind.LogicalOrExpression)
                    => 1 + CalculateComplexity(child, nestingLevel),

                // Ternary operator adds 1 + nesting
                ConditionalExpressionSyntax => 1 + nestingLevel + CalculateComplexity(child, nestingLevel + 1),

                // Recursion/goto/break/continue add 1 for flow interruption
                GotoStatementSyntax => 1,
                BreakStatementSyntax when !IsInSwitch(child) => 1,
                ContinueStatementSyntax => 1,

                // Lambda/anonymous methods increase nesting
                LambdaExpressionSyntax => CalculateComplexity(child, nestingLevel + 1),
                AnonymousMethodExpressionSyntax => CalculateComplexity(child, nestingLevel + 1),

                // Recurse for other nodes without incrementing
                _ => CalculateComplexity(child, nestingLevel)
            };
        }

        return complexity;
    }

    private static bool IsInSwitch(SyntaxNode node)
    {
        return node.Ancestors().Any(a => a is SwitchStatementSyntax);
    }
}
