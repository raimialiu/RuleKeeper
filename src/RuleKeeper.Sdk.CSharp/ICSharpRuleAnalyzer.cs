namespace RuleKeeper.Sdk.CSharp;

/// <summary>
/// Interface for C#-specific rule analyzers that use Roslyn for analysis.
/// </summary>
public interface ICSharpRuleAnalyzer
{
    /// <summary>
    /// The unique identifier for this rule.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// The display name for this rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// The category this rule belongs to.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// A description of what this rule checks.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The default severity level for this rule.
    /// </summary>
    SeverityLevel DefaultSeverity { get; }

    /// <summary>
    /// Initializes the analyzer with configuration parameters.
    /// </summary>
    /// <param name="parameters">Configuration parameters for the rule.</param>
    void Initialize(Dictionary<string, object> parameters);

    /// <summary>
    /// Analyzes the given C# context and returns any violations found.
    /// </summary>
    /// <param name="context">The analysis context containing the Roslyn syntax tree and semantic model.</param>
    /// <returns>Enumerable of violations found.</returns>
    IEnumerable<Violation> Analyze(CSharpAnalysisContext context);
}
