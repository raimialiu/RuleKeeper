namespace RuleKeeper.Sdk.Attributes;

/// <summary>
/// Marks a class as a RuleKeeper rule analyzer.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RuleAttribute : Attribute
{
    /// <summary>
    /// The unique identifier for this rule.
    /// </summary>
    public string RuleId { get; }

    /// <summary>
    /// The display name for this rule.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A description of what this rule checks.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The default severity level for this rule.
    /// </summary>
    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;

    /// <summary>
    /// The category this rule belongs to (e.g., "Naming", "Security", "Async").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Creates a new RuleAttribute with the specified rule ID.
    /// </summary>
    /// <param name="ruleId">The unique identifier for this rule.</param>
    public RuleAttribute(string ruleId)
    {
        RuleId = ruleId ?? throw new ArgumentNullException(nameof(ruleId));
    }
}
