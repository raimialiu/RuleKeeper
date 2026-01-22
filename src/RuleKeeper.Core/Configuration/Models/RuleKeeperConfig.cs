using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Root configuration model for RuleKeeper.
/// </summary>
public class RuleKeeperConfig
{
    /// <summary>
    /// Configuration version for compatibility checking.
    /// </summary>
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Metadata about this configuration.
    /// </summary>
    [YamlMember(Alias = "metadata")]
    public MetadataConfig? Metadata { get; set; }

    /// <summary>
    /// Scan configuration settings.
    /// </summary>
    [YamlMember(Alias = "scan")]
    public ScanConfig Scan { get; set; } = new();

    /// <summary>
    /// Output and reporting configuration.
    /// </summary>
    [YamlMember(Alias = "output")]
    public OutputConfig Output { get; set; } = new();

    /// <summary>
    /// Coding standards organized by category.
    /// </summary>
    [YamlMember(Alias = "coding_standards")]
    public Dictionary<string, CategoryConfig> CodingStandards { get; set; } = new();

    /// <summary>
    /// Pre-built policy configurations to enable.
    /// </summary>
    [YamlMember(Alias = "prebuilt_policies")]
    public Dictionary<string, PrebuiltPolicyConfig> PrebuiltPolicies { get; set; } = new();

    /// <summary>
    /// Custom validator configurations.
    /// </summary>
    [YamlMember(Alias = "custom_validators")]
    public Dictionary<string, CustomValidatorConfig> CustomValidators { get; set; } = new();

    /// <summary>
    /// Paths to custom rule assemblies.
    /// </summary>
    [YamlMember(Alias = "custom_rules")]
    public List<CustomRuleSource> CustomRules { get; set; } = new();

    /// <summary>
    /// Paths to custom fix provider assemblies.
    /// If not specified, fix providers will be loaded from custom_rules assemblies.
    /// </summary>
    [YamlMember(Alias = "custom_fix_providers")]
    public List<CustomRuleSource> CustomFixProviders { get; set; } = new();
}

/// <summary>
/// Metadata about the configuration.
/// </summary>
public class MetadataConfig
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "author")]
    public string? Author { get; set; }

    [YamlMember(Alias = "created")]
    public DateTime? Created { get; set; }

    [YamlMember(Alias = "updated")]
    public DateTime? Updated { get; set; }
}

/// <summary>
/// Source for custom rules.
/// </summary>
public class CustomRuleSource
{
    [YamlMember(Alias = "path")]
    public string? Path { get; set; }

    [YamlMember(Alias = "nuget")]
    public string? NuGet { get; set; }
}
