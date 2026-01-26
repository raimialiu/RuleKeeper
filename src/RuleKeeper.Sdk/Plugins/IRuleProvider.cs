namespace RuleKeeper.Sdk.Plugins;

/// <summary>
/// Interface for external DLL-based rule providers.
/// Plugins implement this interface to provide custom rules that can be loaded dynamically.
/// </summary>
public interface IRuleProvider
{
    /// <summary>
    /// Gets metadata about this rule provider.
    /// </summary>
    RuleProviderMetadata Metadata { get; }

    /// <summary>
    /// Initialize the provider with configuration.
    /// Called once when the plugin is loaded.
    /// </summary>
    /// <param name="configuration">Configuration dictionary from YAML.</param>
    void Initialize(Dictionary<string, object>? configuration);

    /// <summary>
    /// Get all rule analyzers provided by this plugin.
    /// </summary>
    /// <returns>Collection of rule analyzers.</returns>
    IEnumerable<IRuleAnalyzer> GetRuleAnalyzers();

    /// <summary>
    /// Get all cross-language rules provided by this plugin.
    /// </summary>
    /// <returns>Collection of cross-language rules.</returns>
    IEnumerable<Rules.ICrossLanguageRule> GetCrossLanguageRules();
}

/// <summary>
/// Base class for rule providers with common functionality.
/// </summary>
public abstract class RuleProviderBase : IRuleProvider
{
    public abstract RuleProviderMetadata Metadata { get; }

    protected Dictionary<string, object> Configuration { get; private set; } = new();

    public virtual void Initialize(Dictionary<string, object>? configuration)
    {
        Configuration = configuration ?? new Dictionary<string, object>();
    }

    public abstract IEnumerable<IRuleAnalyzer> GetRuleAnalyzers();

    public virtual IEnumerable<Rules.ICrossLanguageRule> GetCrossLanguageRules()
    {
        return Enumerable.Empty<Rules.ICrossLanguageRule>();
    }

    /// <summary>
    /// Get a configuration value with default.
    /// </summary>
    protected T GetConfig<T>(string key, T defaultValue)
    {
        if (Configuration.TryGetValue(key, out var value))
        {
            if (value is T typed)
                return typed;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}

/// <summary>
/// Attribute to mark a class as a rule provider for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RuleProviderAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for the provider.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Display name of the provider.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of what rules this provider offers.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version of the provider.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Author or organization.
    /// </summary>
    public string? Author { get; set; }

    public RuleProviderAttribute(string providerId)
    {
        ProviderId = providerId;
    }
}
