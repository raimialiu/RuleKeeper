using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Exceptions;

/// <summary>
/// Detects catching of base Exception class.
/// </summary>
[Rule("CS-EXC-002",
    Name = "Avoid Catching Base Exception",
    Description = "Avoid catching the base Exception class",
    Severity = SeverityLevel.Medium,
    Category = "exceptions")]
public class CatchingBaseExceptionAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_rethrow", Description = "Allow catching Exception if it's rethrown", DefaultValue = true)]
    public bool AllowRethrow { get; set; } = true;

    [RuleParameter("allow_logging", Description = "Allow catching Exception if it's logged", DefaultValue = true)]
    public bool AllowLogging { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var catchClauses = root.DescendantNodes()
            .OfType<CatchClauseSyntax>();

        foreach (var catchClause in catchClauses)
        {
            var declaration = catchClause.Declaration;

            // Catch-all without type
            if (declaration == null)
            {
                yield return CreateViolation(
                    catchClause.CatchKeyword.GetLocation(),
                    "Avoid using catch-all without exception type",
                    context,
                    "Specify the exception type to catch"
                );
                continue;
            }

            var exceptionType = declaration.Type?.ToString();
            if (exceptionType != "Exception" && exceptionType != "System.Exception")
                continue;

            // Check if exception is rethrown
            if (AllowRethrow && HasRethrow(catchClause))
                continue;

            // Check if exception is logged
            if (AllowLogging && HasLogging(catchClause))
                continue;

            yield return CreateViolation(
                declaration.Type?.GetLocation() ?? catchClause.CatchKeyword.GetLocation(),
                "Avoid catching the base Exception class without rethrowing or logging",
                context,
                "Catch specific exception types instead of the base Exception class"
            );
        }
    }

    private static bool HasRethrow(CatchClauseSyntax catchClause)
    {
        return catchClause.Block?.Statements
            .OfType<ThrowStatementSyntax>()
            .Any() ?? false;
    }

    private static bool HasLogging(CatchClauseSyntax catchClause)
    {
        var blockText = catchClause.Block?.ToString().ToLowerInvariant() ?? "";
        return blockText.Contains("log") ||
               blockText.Contains("logger") ||
               blockText.Contains("console.write");
    }
}
