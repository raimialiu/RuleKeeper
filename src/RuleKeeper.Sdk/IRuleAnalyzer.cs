namespace RuleKeeper.Sdk;

/// <summary>
/// Interface for rule analyzers that inspect code.
/// </summary>
/// <remarks>
/// This interface supports C# analysis using Roslyn's AnalysisContext.
/// For cross-language rules, see <c>RuleKeeper.Sdk.Rules.ICrossLanguageRule</c>.
/// </remarks>
public interface IRuleAnalyzer
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
    /// The languages this rule supports.
    /// </summary>
    IEnumerable<Language> SupportedLanguages { get; }

    /// <summary>
    /// Initializes the analyzer with configuration parameters.
    /// </summary>
    /// <param name="parameters">Configuration parameters for the rule.</param>
    void Initialize(Dictionary<string, object> parameters);

    /// <summary>
    /// Analyzes the given context and returns any violations found.
    /// </summary>
    /// <param name="context">The analysis context containing the syntax tree and semantic model.</param>
    /// <returns>Enumerable of violations found.</returns>
    IEnumerable<Violation> Analyze(AnalysisContext context);
}
