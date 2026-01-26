using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for combining multiple patterns/queries with AND/OR/NONE logic.
/// </summary>
public class MatchConfig
{
    /// <summary>
    /// All conditions must match (AND logic).
    /// </summary>
    [YamlMember(Alias = "all")]
    public List<MatchCondition>? All { get; set; }

    /// <summary>
    /// At least one condition must match (OR logic).
    /// </summary>
    [YamlMember(Alias = "any")]
    public List<MatchCondition>? Any { get; set; }

    /// <summary>
    /// None of these conditions should match (NOT/NONE logic).
    /// </summary>
    [YamlMember(Alias = "none")]
    public List<MatchCondition>? None { get; set; }

    /// <summary>
    /// Minimum number of 'any' conditions that must match.
    /// Default is 1 when 'any' is specified.
    /// </summary>
    [YamlMember(Alias = "min_any_matches")]
    public int MinAnyMatches { get; set; } = 1;

    /// <summary>
    /// Whether to short-circuit evaluation (stop at first match/fail).
    /// Default is true for performance.
    /// </summary>
    [YamlMember(Alias = "short_circuit")]
    public bool ShortCircuit { get; set; } = true;
}

/// <summary>
/// A single match condition that can be a pattern, AST query, or nested match.
/// </summary>
public class MatchCondition
{
    /// <summary>
    /// Pattern-based condition.
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public PatternConfig? Pattern { get; set; }

    /// <summary>
    /// AST query-based condition.
    /// </summary>
    [YamlMember(Alias = "ast_query")]
    public AstQueryConfig? AstQuery { get; set; }

    /// <summary>
    /// Nested match condition for complex logic.
    /// </summary>
    [YamlMember(Alias = "match")]
    public MatchConfig? Match { get; set; }

    /// <summary>
    /// Expression-based condition.
    /// </summary>
    [YamlMember(Alias = "expression")]
    public string? Expression { get; set; }

    /// <summary>
    /// Negate this condition (NOT).
    /// </summary>
    [YamlMember(Alias = "negate")]
    public bool Negate { get; set; } = false;

    /// <summary>
    /// Label for this condition (for debugging/messages).
    /// </summary>
    [YamlMember(Alias = "label")]
    public string? Label { get; set; }
}
