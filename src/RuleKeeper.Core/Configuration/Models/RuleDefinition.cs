using RuleKeeper.Sdk;
using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Defines configuration for a single rule.
/// </summary>
public class RuleDefinition
{
    /// <summary>
    /// The unique identifier for the rule.
    /// </summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>
    /// Display name for the rule.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Description of what the rule checks.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Severity level for violations of this rule.
    /// </summary>
    [YamlMember(Alias = "severity")]
    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;

    /// <summary>
    /// Whether this rule is enabled.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to skip this rule (alias for enabled=false).
    /// </summary>
    [YamlMember(Alias = "skip")]
    public bool Skip { get; set; } = false;

    /// <summary>
    /// Regex pattern for valid code (must match).
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// Regex pattern for invalid code (must not match).
    /// </summary>
    [YamlMember(Alias = "anti_pattern")]
    public string? AntiPattern { get; set; }

    /// <summary>
    /// List of syntax elements this rule applies to.
    /// </summary>
    [YamlMember(Alias = "applies_to")]
    public List<string> AppliesTo { get; set; } = new();

    /// <summary>
    /// File pattern to limit which files this rule applies to.
    /// </summary>
    [YamlMember(Alias = "file_pattern")]
    public string? FilePattern { get; set; }

    /// <summary>
    /// Name of a custom validator to use.
    /// </summary>
    [YamlMember(Alias = "custom_validator")]
    public string? CustomValidator { get; set; }

    /// <summary>
    /// Message to display when the rule is violated.
    /// </summary>
    [YamlMember(Alias = "message")]
    public string? Message { get; set; }

    /// <summary>
    /// Hint for how to fix violations of this rule.
    /// </summary>
    [YamlMember(Alias = "fix_hint")]
    public string? FixHint { get; set; }

    /// <summary>
    /// Additional parameters for the rule.
    /// </summary>
    [YamlMember(Alias = "parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// File patterns to exclude for this specific rule.
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Returns true if this rule is effectively enabled.
    /// </summary>
    public bool IsEnabled => Enabled && !Skip;
}
