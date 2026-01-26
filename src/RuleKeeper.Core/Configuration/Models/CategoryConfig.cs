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
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "skip")]
    public bool Skip { get; set; } = false;

    [YamlMember(Alias = "severity")]
    public SeverityLevel? Severity { get; set; }

    [YamlIgnore]
    public List<RuleDefinition> Rules { get; set; } = new();

    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    [YamlMember(Alias = "skip_rules")]
    public List<string> SkipRules { get; set; } = new();

    public bool IsEnabled => Enabled && !Skip;

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
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "skip")]
    public bool Skip { get; set; } = false;

    [YamlMember(Alias = "severity")]
    public SeverityLevel? Severity { get; set; }

    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    [YamlMember(Alias = "skip_rules")]
    public List<string> SkipRules { get; set; } = new();

    public bool IsEnabled => Enabled && !Skip;
}

/// <summary>
/// Configuration for a custom validator.
/// </summary>
public class CustomValidatorConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "skip")]
    public bool Skip { get; set; } = false;

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "regex";

    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    [YamlMember(Alias = "regex")]
    public string? Regex { get; set; }

    [YamlMember(Alias = "options")]
    public List<string> Options { get; set; } = new();

    [YamlMember(Alias = "message_template")]
    public string? MessageTemplate { get; set; }

    [YamlMember(Alias = "severity")]
    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;

    [YamlMember(Alias = "script")]
    public string? Script { get; set; }

    [YamlMember(Alias = "assembly")]
    public string? Assembly { get; set; }

    [YamlMember(Alias = "type_name")]
    public string? TypeName { get; set; }

    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    public bool IsEnabled => Enabled && !Skip;

    public string? GetPattern() => Regex ?? Pattern;
}
