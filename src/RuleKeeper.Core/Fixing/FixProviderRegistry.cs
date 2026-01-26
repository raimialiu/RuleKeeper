using System.Reflection;
using RuleKeeper.Core.Fixing.Providers;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Core.Fixing;

/// <summary>
/// Registry for all available fix providers.
/// </summary>
public static class FixProviderRegistry
{
    private static readonly List<IFixProvider> _providers = new();
    private static readonly HashSet<string> _loadedAssemblies = new();
    private static bool _initialized;

    /// <summary>
    /// Gets all registered fix providers.
    /// </summary>
    public static IReadOnlyList<IFixProvider> Providers
    {
        get
        {
            EnsureInitialized();
            return _providers.AsReadOnly();
        }
    }

    /// <summary>
    /// Initializes the registry with built-in providers.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // Register built-in fix providers
        _providers.Add(new NamingFixProvider());
        _providers.Add(new AsyncFixProvider());
        _providers.Add(new ExceptionFixProvider());

        _initialized = true;
    }

    /// <summary>
    /// Registers a custom fix provider.
    /// </summary>
    public static void Register(IFixProvider provider)
    {
        EnsureInitialized();
        _providers.Add(provider);
    }

    /// <summary>
    /// Loads fix providers from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for fix providers.</param>
    /// <returns>Number of providers loaded.</returns>
    public static int RegisterAssembly(Assembly assembly)
    {
        EnsureInitialized();

        var assemblyName = assembly.FullName ?? assembly.GetName().Name ?? "";
        if (_loadedAssemblies.Contains(assemblyName))
            return 0;

        _loadedAssemblies.Add(assemblyName);

        var count = 0;
        var fixProviderTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract &&
                       !t.IsInterface &&
                       typeof(IFixProvider).IsAssignableFrom(t) &&
                       t.GetCustomAttribute<FixProviderAttribute>() != null);

        foreach (var type in fixProviderTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IFixProvider provider)
                {
                    _providers.Add(provider);
                    count++;
                }
            }
            catch
            {
            }
        }

        return count;
    }

    /// <summary>
    /// Loads fix providers from an assembly file.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>Number of providers loaded.</returns>
    public static int LoadFromFile(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            return 0;

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return RegisterAssembly(assembly);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Loads fix providers from multiple assembly files.
    /// </summary>
    /// <param name="assemblyPaths">Paths to assembly files.</param>
    /// <returns>Total number of providers loaded.</returns>
    public static int LoadFromFiles(IEnumerable<string> assemblyPaths)
    {
        return assemblyPaths.Sum(LoadFromFile);
    }

    /// <summary>
    /// Gets providers that can fix the specified rule.
    /// </summary>
    public static IEnumerable<IFixProvider> GetProvidersForRule(string ruleId)
    {
        EnsureInitialized();
        return _providers.Where(p => p.SupportedRuleIds.Contains(ruleId));
    }

    /// <summary>
    /// Gets all supported rule IDs.
    /// </summary>
    public static IEnumerable<string> GetAllSupportedRuleIds()
    {
        EnsureInitialized();
        return _providers.SelectMany(p => p.SupportedRuleIds).Distinct().OrderBy(id => id);
    }

    /// <summary>
    /// Gets information about all registered providers.
    /// </summary>
    public static IEnumerable<FixProviderInfo> GetProviderInfo()
    {
        EnsureInitialized();
        return _providers.Select(p =>
        {
            var attr = p.GetType().GetCustomAttribute<FixProviderAttribute>();
            return new FixProviderInfo
            {
                Name = attr?.Name ?? p.GetType().Name,
                Description = attr?.Description ?? "",
                Category = attr?.Category ?? "",
                SupportedRuleIds = p.SupportedRuleIds.ToList(),
                TypeName = p.GetType().FullName ?? p.GetType().Name,
                IsBuiltIn = p.GetType().Assembly == typeof(FixProviderRegistry).Assembly
            };
        });
    }

    /// <summary>
    /// Creates a configured CodeFixer with all registered providers.
    /// </summary>
    public static CodeFixer CreateFixer(bool createBackups = true, bool dryRun = false)
    {
        EnsureInitialized();
        var fixer = new CodeFixer(createBackups, dryRun);
        foreach (var provider in _providers)
        {
            fixer.RegisterProvider(provider);
        }
        return fixer;
    }

    private static void EnsureInitialized()
    {
        if (!_initialized) Initialize();
    }

    /// <summary>
    /// Resets the registry (mainly for testing).
    /// </summary>
    public static void Reset()
    {
        _providers.Clear();
        _loadedAssemblies.Clear();
        _initialized = false;
    }
}

/// <summary>
/// Information about a registered fix provider.
/// </summary>
public class FixProviderInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> SupportedRuleIds { get; set; } = new();
    public string TypeName { get; set; } = "";
    public bool IsBuiltIn { get; set; }
}
