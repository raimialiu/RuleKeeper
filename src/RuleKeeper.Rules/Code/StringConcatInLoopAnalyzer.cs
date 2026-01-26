using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects string concatenation inside loops which causes
/// O(n²) memory allocations. Use StringBuilder instead.
/// </summary>
[Rule("CS-CODE-009",
    Name = "String Concatenation in Loop",
    Description = "Avoid string concatenation in loops; use StringBuilder",
    Severity = SeverityLevel.Medium,
    Category = "code")]
public class StringConcatInLoopAnalyzer : BaseRuleAnalyzer
{
    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var semanticModel = context.SemanticModel;

        // Find all loops
        var loops = root.DescendantNodes()
            .Where(n => n is ForStatementSyntax or
                        ForEachStatementSyntax or
                        WhileStatementSyntax or
                        DoStatementSyntax);

        foreach (var loop in loops)
        {
            // Find string concatenation assignments
            var assignments = loop.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(a => a.IsKind(SyntaxKind.AddAssignmentExpression) ||
                            a.IsKind(SyntaxKind.SimpleAssignmentExpression));

            foreach (var assignment in assignments)
            {
                // Check if it's string += or str = str + ...
                if (IsStringConcatenation(assignment, semanticModel))
                {
                    yield return CreateViolation(
                        assignment.GetLocation(),
                        "String concatenation in loop causes O(n²) allocations",
                        context,
                        "Use StringBuilder: var sb = new StringBuilder(); ... sb.Append(value);"
                    );
                }
            }

            // Also check for string interpolation in loops
            var interpolations = loop.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(a => a.Right is InterpolatedStringExpressionSyntax);

            foreach (var interpolation in interpolations)
            {
                if (IsStringVariable(interpolation.Left, semanticModel))
                {
                    yield return CreateViolation(
                        interpolation.GetLocation(),
                        "String interpolation assignment in loop causes allocations",
                        context,
                        "Use StringBuilder with AppendFormat or Append"
                    );
                }
            }
        }
    }

    private static bool IsStringConcatenation(AssignmentExpressionSyntax assignment, SemanticModel? semanticModel)
    {
        if (semanticModel == null)
            return false;

        // Check += with string
        if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
        {
            return IsStringVariable(assignment.Left, semanticModel);
        }

        // Check str = str + something
        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
            assignment.Right is BinaryExpressionSyntax binary &&
            binary.IsKind(SyntaxKind.AddExpression))
        {
            return IsStringVariable(assignment.Left, semanticModel) &&
                   IsStringExpression(binary, semanticModel);
        }

        return false;
    }

    private static bool IsStringVariable(ExpressionSyntax expression, SemanticModel? semanticModel)
    {
        if (semanticModel == null)
            return false;

        var typeInfo = semanticModel.GetTypeInfo(expression);
        return typeInfo.Type?.SpecialType == SpecialType.System_String;
    }

    private static bool IsStringExpression(BinaryExpressionSyntax binary, SemanticModel? semanticModel)
    {
        if (semanticModel == null)
            return false;

        var typeInfo = semanticModel.GetTypeInfo(binary);
        return typeInfo.Type?.SpecialType == SpecialType.System_String;
    }
}
