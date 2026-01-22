using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Security;

/// <summary>
/// Detects potential path traversal vulnerabilities.
/// </summary>
[Rule("CS-SEC-005",
    Name = "Path Traversal Prevention",
    Description = "Detects potential path traversal vulnerabilities",
    Severity = SeverityLevel.High,
    Category = "security")]
public class PathTraversalAnalyzer : BaseRuleAnalyzer
{
    private static readonly string[] FileOperationMethods = new[]
    {
        "ReadAllText", "ReadAllBytes", "ReadAllLines", "ReadLines",
        "WriteAllText", "WriteAllBytes", "WriteAllLines",
        "Open", "OpenRead", "OpenWrite", "OpenText",
        "Create", "CreateText", "Delete", "Move", "Copy",
        "Exists", "GetAttributes"
    };

    private static readonly string[] PathMethods = new[]
    {
        "Combine", "Join", "GetFullPath"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            if (methodName == null)
                continue;

            // Check file operations
            if (FileOperationMethods.Contains(methodName))
            {
                var pathArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                if (pathArg != null && ContainsUserInput(pathArg.Expression))
                {
                    yield return CreateViolation(
                        invocation.GetLocation(),
                        $"Potential path traversal in '{methodName}': validate and sanitize file paths",
                        context,
                        "Use Path.GetFullPath and validate the result is within allowed directories"
                    );
                }
            }

            // Check Path.Combine with user input
            if (PathMethods.Contains(methodName))
            {
                foreach (var arg in invocation.ArgumentList.Arguments)
                {
                    if (ContainsUserInput(arg.Expression) &&
                        !HasPathValidation(invocation.Parent))
                    {
                        yield return CreateViolation(
                            invocation.GetLocation(),
                            $"Path.{methodName} with user input: ensure proper validation",
                            context,
                            "Validate that the resulting path doesn't escape allowed directories"
                        );
                        break;
                    }
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

    private static bool ContainsUserInput(ExpressionSyntax expression)
    {
        // Heuristic: consider parameters and certain property accesses as potential user input
        foreach (var node in expression.DescendantNodesAndSelf())
        {
            if (node is IdentifierNameSyntax identifier)
            {
                var name = identifier.Identifier.Text.ToLowerInvariant();
                if (name.Contains("path") || name.Contains("file") || name.Contains("name") ||
                    name.Contains("input") || name.Contains("request") || name.Contains("param"))
                {
                    return true;
                }
            }

            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var fullPath = memberAccess.ToString().ToLowerInvariant();
                if (fullPath.Contains("request") || fullPath.Contains("query") ||
                    fullPath.Contains("form") || fullPath.Contains("route"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasPathValidation(Microsoft.CodeAnalysis.SyntaxNode? node)
    {
        // Simple heuristic: check if there's validation nearby
        if (node == null) return false;

        var text = node.ToString().ToLowerInvariant();
        return text.Contains("startswith") || text.Contains("validate") ||
               text.Contains("sanitize") || text.Contains("getfullpath");
    }
}
