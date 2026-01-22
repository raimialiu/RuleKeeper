using RuleKeeper.Sdk;
using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for a category of rules.
/// Supports two YAML formats:
/// 1. Simple list format: category_name: [- id: ..., - id: ...]
/// 2. Full format: category_name: { enabled: true, severity: High, rules: [...] }
/// </summary>
public class CategoryConfig
{
    /// <summary>
    /// Whether this entire category is enabled.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default severity for rules in this category.
    /// </summary>
    [YamlMember(Alias = "severity")]
    public SeverityLevel? Severity { get; set; }

    /// <summary>
    /// Rules in this category as a list.
    /// </summary>
    [YamlIgnore]
    public List<RuleDefinition> Rules { get; set; } = new();

    /// <summary>
    /// File patterns to exclude for this category.
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Creates a CategoryConfig from a list of rule definitions (simple format).
    /// </summary>
    public static CategoryConfig FromRules(List<RuleDefinition> rules)
    {
        return new CategoryConfig { Rules = rules ?? new List<RuleDefinition>() };
    }
}

/// <summary>
/// Configuration for a pre-built policy.
/// </summary>
public class PrebuiltPolicyConfig
{
    /// <summary>
    /// Whether this policy is enabled.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Override severity for this policy.
    /// </summary>
    [YamlMember(Alias = "severity")]
    public SeverityLevel? Severity { get; set; }

    /// <summary>
    /// File patterns to exclude for this policy.
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Rules to skip within this policy.
    /// </summary>
    [YamlMember(Alias = "skip_rules")]
    public List<string> SkipRules { get; set; } = new();
}

/// <summary>
/// Configuration for a custom validator.
/// </summary>
public class CustomValidatorConfig
{
    /// <summary>
    /// Description of what this validator checks.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of validator (regex, roslyn, script).
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "regex";

    /// <summary>
    /// Pattern for regex validators.
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// Script content for script validators.
    /// </summary>
    [YamlMember(Alias = "script")]
    public string? Script { get; set; }

    /// <summary>
    /// Path to external validator assembly.
    /// </summary>
    [YamlMember(Alias = "assembly")]
    public string? Assembly { get; set; }

    /// <summary>
    /// Type name in the assembly.
    /// </summary>
    [YamlMember(Alias = "type_name")]
    public string? TypeName { get; set; }
}
