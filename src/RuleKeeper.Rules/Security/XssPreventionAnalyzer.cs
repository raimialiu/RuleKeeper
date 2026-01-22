using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Security;

/// <summary>
/// Detects potential XSS vulnerabilities.
/// </summary>
[Rule("CS-SEC-004",
    Name = "XSS Prevention",
    Description = "Detects potential Cross-Site Scripting vulnerabilities",
    Severity = SeverityLevel.High,
    Category = "security")]
public class XssPreventionAnalyzer : BaseRuleAnalyzer
{
    private static readonly string[] DangerousMethods = new[]
    {
        "Html.Raw", "HtmlString", "MvcHtmlString", "WriteHtml"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var fullMethodName = GetFullMethodName(invocation);
            if (fullMethodName == null)
                continue;

            // Check for Html.Raw usage
            if (DangerousMethods.Any(m => fullMethodName.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                // Check if the argument is user input
                var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
                if (argument != null && !IsStaticContent(argument.Expression))
                {
                    yield return CreateViolation(
                        invocation.GetLocation(),
                        $"Potential XSS vulnerability: {fullMethodName} with dynamic content",
                        context,
                        "Ensure user input is properly encoded or use safe alternatives"
                    );
                }
            }
        }

        // Check for dangerous Razor patterns in string literals
        var literals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>();

        foreach (var literal in literals)
        {
            var text = literal.Token.ValueText ?? "";
            if (text.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("onerror=", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("onclick=", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is part of a concatenation with variables
                var parent = literal.Parent;
                if (parent is BinaryExpressionSyntax ||
                    parent is InterpolatedStringTextSyntax)
                {
                    yield return CreateViolation(
                        literal.GetLocation(),
                        "Potential XSS vulnerability: inline JavaScript in dynamic string",
                        context,
                        "Use proper encoding and avoid inline JavaScript"
                    );
                }
            }
        }
    }

    private static string? GetFullMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess =>
                $"{memberAccess.Expression}.{memberAccess.Name.Identifier.Text}",
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static bool IsStaticContent(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax;
    }
}
