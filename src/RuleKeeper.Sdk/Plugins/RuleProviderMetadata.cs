namespace RuleKeeper.Sdk.Plugins;

/// <summary>
/// Metadata about a rule provider plugin.
/// </summary>
public class RuleProviderMetadata
{
    /// <summary>
    /// Unique identifier for the provider.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Display name of the provider.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what rules this provider offers.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version of the provider.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Author or organization name.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// URL for more information.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Languages supported by rules in this provider.
    /// Empty means all languages.
    /// </summary>
    public Language[] SupportedLanguages { get; init; } = Array.Empty<Language>();

    /// <summary>
    /// Categories of rules provided.
    /// </summary>
    public string[] Categories { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Minimum RuleKeeper version required.
    /// </summary>
    public string? MinRuleKeeperVersion { get; init; }

    /// <summary>
    /// Additional metadata properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Create metadata from a RuleProviderAttribute.
    /// </summary>
    public static RuleProviderMetadata FromAttribute(RuleProviderAttribute attribute)
    {
        return new RuleProviderMetadata
        {
            ProviderId = attribute.ProviderId,
            Name = attribute.Name ?? attribute.ProviderId,
            Description = attribute.Description,
            Version = attribute.Version ?? "1.0.0",
            Author = attribute.Author
        };
    }
}

/// <summary>
/// Information about a loaded plugin.
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// The provider metadata.
    /// </summary>
    public required RuleProviderMetadata Metadata { get; init; }

    /// <summary>
    /// The provider instance.
    /// </summary>
    public required IRuleProvider Provider { get; init; }

    /// <summary>
    /// Path to the assembly file.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    /// When the plugin was loaded.
    /// </summary>
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Number of rules provided.
    /// </summary>
    public int RuleCount { get; init; }

    /// <summary>
    /// Whether the plugin loaded successfully.
    /// </summary>
    public bool IsLoaded { get; init; } = true;

    /// <summary>
    /// Error message if loading failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
