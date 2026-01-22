using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Security;

/// <summary>
/// Detects potential SQL injection vulnerabilities.
/// </summary>
[Rule("CS-SEC-001",
    Name = "SQL Injection Prevention",
    Description = "Detects potential SQL injection vulnerabilities from string concatenation",
    Severity = SeverityLevel.Critical,
    Category = "security")]
public class SqlInjectionAnalyzer : BaseRuleAnalyzer
{
    private static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE",
        "ALTER", "TRUNCATE", "EXEC", "EXECUTE", "FROM", "WHERE"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        // Check for string concatenation with SQL keywords
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            var text = literal.Token.ValueText?.ToUpperInvariant() ?? "";
            if (!ContainsSqlKeyword(text))
                continue;

            // Check if this is part of a concatenation or interpolation
            var parent = literal.Parent;
            while (parent != null)
            {
                if (parent is BinaryExpressionSyntax binary &&
                    binary.OperatorToken.Text == "+")
                {
                    // Check if concatenating with a variable
                    if (HasVariableOperand(binary))
                    {
                        yield return CreateViolation(
                            binary.GetLocation(),
                            "Potential SQL injection: avoid string concatenation in SQL queries",
                            context,
                            "Use parameterized queries or stored procedures instead"
                        );
                        break;
                    }
                }

                if (parent is InterpolatedStringExpressionSyntax interpolated)
                {
                    // Check if interpolation includes variables
                    if (interpolated.Contents.OfType<InterpolationSyntax>().Any())
                    {
                        yield return CreateViolation(
                            interpolated.GetLocation(),
                            "Potential SQL injection: avoid string interpolation in SQL queries",
                            context,
                            "Use parameterized queries or stored procedures instead"
                        );
                        break;
                    }
                }

                parent = parent.Parent;
            }
        }

        // Check for ExecuteSqlRaw and similar methods
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodName = GetMethodName(invocation);
            if (methodName == null)
                continue;

            if (IsSqlExecutionMethod(methodName))
            {
                var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
                if (argument?.Expression is BinaryExpressionSyntax ||
                    argument?.Expression is InterpolatedStringExpressionSyntax)
                {
                    yield return CreateViolation(
                        invocation.GetLocation(),
                        $"Potential SQL injection in '{methodName}': use parameterized queries",
                        context,
                        "Use FormattableString or parameters instead of string concatenation"
                    );
                }
            }
        }
    }

    private static bool ContainsSqlKeyword(string text)
    {
        return SqlKeywords.Any(keyword => text.Contains(keyword));
    }

    private static bool HasVariableOperand(BinaryExpressionSyntax binary)
    {
        return IsVariableExpression(binary.Left) || IsVariableExpression(binary.Right);
    }

    private static bool IsVariableExpression(ExpressionSyntax expression)
    {
        return expression is IdentifierNameSyntax ||
               expression is MemberAccessExpressionSyntax ||
               expression is InvocationExpressionSyntax;
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

    private static bool IsSqlExecutionMethod(string methodName)
    {
        return methodName.Contains("Sql", StringComparison.OrdinalIgnoreCase) ||
               methodName.Contains("Query", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("ExecuteNonQuery", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("ExecuteReader", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("ExecuteScalar", StringComparison.OrdinalIgnoreCase);
    }
}
