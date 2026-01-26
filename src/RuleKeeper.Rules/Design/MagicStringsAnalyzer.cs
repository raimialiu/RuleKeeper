using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects magic strings (unexplained string literals) in code.
/// Strings should be named constants or resources to improve maintainability.
/// </summary>
[Rule("CS-DESIGN-009",
    Name = "Magic Strings",
    Description = "Avoid magic strings; use named constants instead",
    Severity = SeverityLevel.Low,
    Category = "design")]
public class MagicStringsAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("min_length", Description = "Minimum string length to flag", DefaultValue = 3)]
    public int MinLength { get; set; } = 3;

    [RuleParameter("ignore_empty", Description = "Ignore empty strings", DefaultValue = true)]
    public bool IgnoreEmpty { get; set; } = true;

    [RuleParameter("ignore_logging", Description = "Ignore strings in logging calls", DefaultValue = true)]
    public bool IgnoreLogging { get; set; } = true;

    [RuleParameter("ignore_exceptions", Description = "Ignore strings in exception messages", DefaultValue = true)]
    public bool IgnoreExceptions { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var literals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(l => l.Kind() == SyntaxKind.StringLiteralExpression);

        foreach (var literal in literals)
        {
            var value = literal.Token.ValueText;

            // Skip empty strings
            if (IgnoreEmpty && string.IsNullOrEmpty(value))
                continue;

            // Skip short strings
            if (value.Length < MinLength)
                continue;

            // Skip if in constant declaration
            if (IsInConstantDeclaration(literal))
                continue;

            // Skip if in attribute
            if (IsInAttribute(literal))
                continue;

            // Skip logging statements
            if (IgnoreLogging && IsInLoggingCall(literal))
                continue;

            // Skip exception messages
            if (IgnoreExceptions && IsInExceptionThrow(literal))
                continue;

            // Skip nameof expressions
            if (IsInNameof(literal))
                continue;

            // Skip interpolated strings (they're usually intentional)
            if (literal.Parent is InterpolatedStringExpressionSyntax)
                continue;

            var truncatedValue = value.Length > 30 ? value.Substring(0, 27) + "..." : value;
            yield return CreateViolation(
                literal.GetLocation(),
                $"Magic string \"{truncatedValue}\" should be replaced with a named constant",
                context,
                $"Create a constant: private const string MEANINGFUL_NAME = \"{truncatedValue}\";"
            );
        }
    }

    private static bool IsInConstantDeclaration(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is FieldDeclarationSyntax field)
            {
                return field.Modifiers.Any(SyntaxKind.ConstKeyword) ||
                       field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
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

    private static bool IsInLoggingCall(SyntaxNode node)
    {
        var invocation = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation == null) return false;

        var methodName = invocation.Expression.ToString().ToLowerInvariant();
        return methodName.Contains("log") ||
               methodName.Contains("trace") ||
               methodName.Contains("debug") ||
               methodName.Contains("console.write");
    }

    private static bool IsInExceptionThrow(SyntaxNode node)
    {
        return node.Ancestors().Any(a => a is ThrowExpressionSyntax or ThrowStatementSyntax) ||
               node.Ancestors().OfType<ObjectCreationExpressionSyntax>()
                   .Any(o => o.Type.ToString().EndsWith("Exception"));
    }

    private static bool IsInNameof(SyntaxNode node)
    {
        return node.Ancestors().OfType<InvocationExpressionSyntax>()
            .Any(i => i.Expression.ToString() == "nameof");
    }
}
