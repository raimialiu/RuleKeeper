using System.Reflection;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Core.Rules;

/// <summary>
/// Registry for discovering and managing rule analyzers.
/// </summary>
public class RuleRegistry
{
    private readonly Dictionary<string, Type> _ruleTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RuleInfo> _ruleInfos = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Assembly> _loadedAssemblies = new();

    /// <summary>
    /// Gets all registered rule IDs.
    /// </summary>
    public IEnumerable<string> RuleIds => _ruleTypes.Keys;

    /// <summary>
    /// Gets information about all registered rules.
    /// </summary>
    public IEnumerable<RuleInfo> Rules => _ruleInfos.Values;

    /// <summary>
    /// Gets the number of registered rules.
    /// </summary>
    public int Count => _ruleTypes.Count;

    /// <summary>
    /// Discovers and registers all rule analyzers from the specified assembly.
    /// </summary>
    public void RegisterAssembly(Assembly assembly)
    {
        if (_loadedAssemblies.Contains(assembly))
            return;

        _loadedAssemblies.Add(assembly);

        var ruleTypes = assembly.GetTypes()
            .Where(t => typeof(IRuleAnalyzer).IsAssignableFrom(t))
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in ruleTypes)
        {
            RegisterType(type);
        }
    }

    /// <summary>
    /// Registers a single rule type.
    /// </summary>
    public void RegisterType(Type type)
    {
        if (!typeof(IRuleAnalyzer).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type.Name} does not implement IRuleAnalyzer");
        }

        var ruleAttribute = type.GetCustomAttribute<RuleAttribute>();
        string ruleId;

        if (ruleAttribute != null)
        {
            ruleId = ruleAttribute.RuleId;
        }
        else
        {
            // Try to create an instance to get the RuleId
            try
            {
                var instance = (IRuleAnalyzer)Activator.CreateInstance(type)!;
                ruleId = instance.RuleId;
            }
            catch
            {
                ruleId = type.Name;
            }
        }

        _ruleTypes[ruleId] = type;
        _ruleInfos[ruleId] = CreateRuleInfo(type, ruleId, ruleAttribute);
    }

    /// <summary>
    /// Registers a rule type with a specific ID.
    /// </summary>
    public void RegisterType<T>(string ruleId) where T : IRuleAnalyzer
    {
        _ruleTypes[ruleId] = typeof(T);
        _ruleInfos[ruleId] = CreateRuleInfo(typeof(T), ruleId, null);
    }

    /// <summary>
    /// Registers a cross-language rule type.
    /// </summary>
    public void RegisterCrossLanguageType(Type type)
    {
        if (!typeof(ICrossLanguageRule).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type.Name} does not implement ICrossLanguageRule");
        }

        var ruleAttribute = type.GetCustomAttribute<RuleAttribute>();
        string ruleId;

        if (ruleAttribute != null)
        {
            ruleId = ruleAttribute.RuleId;
        }
        else
        {
            // Try to create an instance to get the RuleId
            try
            {
                var instance = (ICrossLanguageRule)Activator.CreateInstance(type)!;
                ruleId = instance.RuleId;
            }
            catch
            {
                ruleId = type.Name;
            }
        }

        _ruleTypes[ruleId] = type;
        _ruleInfos[ruleId] = CreateCrossLanguageRuleInfo(type, ruleId, ruleAttribute);
    }

    /// <summary>
    /// Registers a cross-language rule type with a specific ID.
    /// </summary>
    public void RegisterCrossLanguageType<T>(string ruleId) where T : ICrossLanguageRule
    {
        _ruleTypes[ruleId] = typeof(T);
        _ruleInfos[ruleId] = CreateCrossLanguageRuleInfo(typeof(T), ruleId, null);
    }

    /// <summary>
    /// Creates an instance of a rule analyzer by ID.
    /// </summary>
    public IRuleAnalyzer? CreateAnalyzer(string ruleId)
    {
        if (!_ruleTypes.TryGetValue(ruleId, out var type))
        {
            return null;
        }

        return (IRuleAnalyzer)Activator.CreateInstance(type)!;
    }

    /// <summary>
    /// Creates an instance of a rule analyzer by ID with parameters.
    /// </summary>
    public IRuleAnalyzer? CreateAnalyzer(string ruleId, Dictionary<string, object> parameters)
    {
        var analyzer = CreateAnalyzer(ruleId);
        if (analyzer != null)
        {
            analyzer.Initialize(parameters);
        }
        return analyzer;
    }

    /// <summary>
    /// Gets information about a rule by ID.
    /// </summary>
    public RuleInfo? GetRuleInfo(string ruleId)
    {
        return _ruleInfos.TryGetValue(ruleId, out var info) ? info : null;
    }

    /// <summary>
    /// Checks if a rule with the given ID is registered.
    /// </summary>
    public bool HasRule(string ruleId) => _ruleTypes.ContainsKey(ruleId);

    /// <summary>
    /// Gets all rules in a specific category.
    /// </summary>
    public IEnumerable<RuleInfo> GetRulesByCategory(string category)
    {
        return _ruleInfos.Values.Where(r =>
            string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Loads custom rules from the configuration.
    /// </summary>
    public void LoadCustomRules(RuleKeeperConfig config)
    {
        foreach (var source in config.CustomRules)
        {
            if (!string.IsNullOrEmpty(source.Path))
            {
                LoadFromPath(source.Path);
            }
            // NuGet loading would require additional implementation
        }
    }

    /// <summary>
    /// Loads rule assemblies from a file path.
    /// </summary>
    public void LoadFromPath(string path)
    {
        if (File.Exists(path))
        {
            var assembly = Assembly.LoadFrom(path);
            RegisterAssembly(assembly);
        }
        else if (Directory.Exists(path))
        {
            foreach (var dll in Directory.GetFiles(path, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    RegisterAssembly(assembly);
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                }
            }
        }
    }

    private RuleInfo CreateRuleInfo(Type type, string ruleId, RuleAttribute? attr)
    {
        // Get supported languages from attribute
        var supportedLangsAttr = type.GetCustomAttribute<SupportedLanguagesAttribute>();
        var supportedLanguages = supportedLangsAttr?.Languages ?? new[] { Language.CSharp };

        // Check if this is a cross-language rule
        var isCrossLanguage = typeof(ICrossLanguageRule).IsAssignableFrom(type);

        return new RuleInfo
        {
            RuleId = ruleId,
            Name = attr?.Name ?? type.Name,
            Description = attr?.Description ?? "",
            Category = attr?.Category ?? "General",
            DefaultSeverity = attr?.Severity ?? SeverityLevel.Medium,
            Type = type,
            SupportedLanguages = supportedLanguages,
            IsCrossLanguage = isCrossLanguage
        };
    }

    private RuleInfo CreateCrossLanguageRuleInfo(Type type, string ruleId, RuleAttribute? attr)
    {
        // Get supported languages from attribute - cross-language rules support all by default
        var supportedLangsAttr = type.GetCustomAttribute<SupportedLanguagesAttribute>();
        var supportedLanguages = supportedLangsAttr?.Languages ?? new[]
        {
            Language.CSharp,
            Language.Python,
            Language.JavaScript,
            Language.TypeScript,
            Language.Java,
            Language.Go
        };

        return new RuleInfo
        {
            RuleId = ruleId,
            Name = attr?.Name ?? type.Name,
            Description = attr?.Description ?? "",
            Category = attr?.Category ?? "General",
            DefaultSeverity = attr?.Severity ?? SeverityLevel.Medium,
            Type = type,
            SupportedLanguages = supportedLanguages,
            IsCrossLanguage = true
        };
    }
}

/// <summary>
/// Information about a registered rule.
/// </summary>
public class RuleInfo
{
    public required string RuleId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required SeverityLevel DefaultSeverity { get; init; }
    public required Type Type { get; init; }

    /// <summary>
    /// Gets the languages this rule supports.
    /// </summary>
    public Language[] SupportedLanguages { get; init; } = new[] { Language.CSharp };

    /// <summary>
    /// Gets whether this rule is a cross-language rule.
    /// </summary>
    public bool IsCrossLanguage { get; init; }

    /// <summary>
    /// Gets a formatted string of supported languages.
    /// </summary>
    public string SupportedLanguagesDisplay =>
        SupportedLanguages.Length == 0
            ? "All"
            : string.Join(", ", SupportedLanguages.Select(l => l.ToString()));
}
