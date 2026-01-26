using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects magic numbers (unexplained numeric literals) in code.
/// Numbers should be named constants to improve readability and maintainability.
/// </summary>
[Rule("CS-DESIGN-008",
    Name = "Magic Numbers",
    Description = "Avoid magic numbers; use named constants instead",
    Severity = SeverityLevel.Low,
    Category = "design")]
public class MagicNumbersAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allowed_values", Description = "Comma-separated list of allowed values", DefaultValue = "-1,0,1,2")]
    public string AllowedValues { get; set; } = "-1,0,1,2";

    [RuleParameter("ignore_in_tests", Description = "Ignore magic numbers in test files", DefaultValue = true)]
    public bool IgnoreInTests { get; set; } = true;

    [RuleParameter("ignore_array_sizes", Description = "Ignore numbers used as array sizes", DefaultValue = true)]
    public bool IgnoreArraySizes { get; set; } = true;

    private HashSet<string> _allowedSet = new();

    public override void Initialize(Dictionary<string, object> parameters)
    {
        base.Initialize(parameters);
        _allowedSet = AllowedValues
            .Split(',')
            .Select(v => v.Trim())
            .ToHashSet();
    }

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        // Skip test files if configured
        if (IgnoreInTests && IsTestFile(context.FilePath))
            yield break;

        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var literals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(IsNumericLiteral);

        foreach (var literal in literals)
        {
            // Skip if in allowed list
            var value = literal.Token.ValueText;
            if (_allowedSet.Contains(value))
                continue;

            // Skip if it's a constant declaration
            if (IsInConstantDeclaration(literal))
                continue;

            // Skip if it's in an attribute
            if (IsInAttribute(literal))
                continue;

            // Skip array sizes if configured
            if (IgnoreArraySizes && IsArraySize(literal))
                continue;

            // Skip enum values
            if (IsEnumValue(literal))
                continue;

            yield return CreateViolation(
                literal.GetLocation(),
                $"Magic number '{value}' should be replaced with a named constant",
                context,
                $"Create a constant: private const int MEANINGFUL_NAME = {value};"
            );
        }
    }

    private static bool IsNumericLiteral(LiteralExpressionSyntax literal)
    {
        return literal.Kind() == SyntaxKind.NumericLiteralExpression;
    }

    private static bool IsInConstantDeclaration(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is FieldDeclarationSyntax field)
            {
                return field.Modifiers.Any(SyntaxKind.ConstKeyword);
            }
            if (parent is LocalDeclarationStatementSyntax local)
            {
                return local.Modifiers.Any(SyntaxKind.ConstKeyword);
            }
            parent = parent.Parent;
        }
        return false;
    }

    private static bool IsInAttribute(SyntaxNode node)
    {
        return node.Ancestors().Any(a => a is AttributeSyntax);
    }

    private static bool IsArraySize(SyntaxNode node)
    {
        return node.Parent is ArrayRankSpecifierSyntax ||
               node.Ancestors().Any(a => a is ArrayCreationExpressionSyntax);
    }

    private static bool IsEnumValue(SyntaxNode node)
    {
        return node.Ancestors().Any(a => a is EnumMemberDeclarationSyntax);
    }

    private static bool IsTestFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        return fileName.Contains("test") ||
               fileName.Contains("spec") ||
               filePath.Contains("Tests", StringComparison.OrdinalIgnoreCase);
    }
}
