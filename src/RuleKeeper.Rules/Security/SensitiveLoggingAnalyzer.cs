using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Security;

/// <summary>
/// Detects logging of sensitive information.
/// </summary>
[Rule("CS-SEC-003",
    Name = "Sensitive Data Logging",
    Description = "Detects logging of potentially sensitive information",
    Severity = SeverityLevel.High,
    Category = "security")]
public class SensitiveLoggingAnalyzer : BaseRuleAnalyzer
{
    private static readonly string[] LoggingMethods = new[]
    {
        "Log", "LogInformation", "LogWarning", "LogError", "LogDebug", "LogTrace", "LogCritical",
        "WriteLine", "Write", "Info", "Warn", "Error", "Debug", "Trace"
    };

    private static readonly string[] SensitiveIdentifiers = new[]
    {
        "password", "passwd", "pwd", "secret", "token", "apikey", "api_key",
        "credential", "credit", "card", "ssn", "social", "dob", "birth",
        "phone", "email", "address", "salary", "bank", "account"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            if (methodName == null || !IsLoggingMethod(methodName))
                continue;

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                var sensitiveIds = FindSensitiveIdentifiers(argument.Expression);
                foreach (var sensitiveId in sensitiveIds)
                {
                    yield return CreateViolation(
                        argument.GetLocation(),
                        $"Potentially logging sensitive data: '{sensitiveId}'",
                        context,
                        "Avoid logging sensitive information or use structured logging with sensitive data masked"
                    );
                }
            }
        }
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static bool IsLoggingMethod(string methodName)
    {
        return LoggingMethods.Any(m =>
            methodName.Equals(m, StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Log", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> FindSensitiveIdentifiers(ExpressionSyntax expression)
    {
        var identifiers = new List<string>();

        foreach (var node in expression.DescendantNodesAndSelf())
        {
            string? name = node switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                _ => null
            };

            if (name != null && IsSensitiveIdentifier(name))
            {
                identifiers.Add(name);
            }
        }

        return identifiers;
    }

    private static bool IsSensitiveIdentifier(string name)
    {
        var lowerName = name.ToLowerInvariant();
        return SensitiveIdentifiers.Any(s => lowerName.Contains(s));
    }
}
