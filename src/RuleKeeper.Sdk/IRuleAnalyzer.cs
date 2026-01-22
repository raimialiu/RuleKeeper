using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RuleKeeper.Sdk;

/// <summary>
/// Interface for rule analyzers that inspect C# code.
/// </summary>
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

/// <summary>
/// Context for analyzing a single file.
/// </summary>
public record AnalysisContext
{
    /// <summary>
    /// The syntax tree of the file being analyzed.
    /// </summary>
    public required SyntaxTree SyntaxTree { get; init; }

    /// <summary>
    /// The semantic model for the file, if available.
    /// </summary>
    public SemanticModel? SemanticModel { get; init; }

    /// <summary>
    /// The compilation containing the file, if available.
    /// </summary>
    public CSharpCompilation? Compilation { get; init; }

    /// <summary>
    /// The file path of the file being analyzed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The severity level to use for violations.
    /// </summary>
    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    /// <summary>
    /// Custom message override for violations.
    /// </summary>
    public string? CustomMessage { get; init; }

    /// <summary>
    /// Fix hint for violations.
    /// </summary>
    public string? FixHint { get; init; }

    /// <summary>
    /// Cancellation token for the analysis.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}
