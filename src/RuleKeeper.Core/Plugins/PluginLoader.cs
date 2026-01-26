using System.Reflection;
using RuleKeeper.Core.Rules;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Plugins;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Core.Plugins;

/// <summary>
/// Loads and manages rule provider plugins from external assemblies.
/// </summary>
public class PluginLoader
{
    private readonly Dictionary<string, PluginInfo> _loadedPlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly List<string> _searchPaths = new();

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IReadOnlyCollection<PluginInfo> LoadedPlugins => _loadedPlugins.Values;

    /// <summary>
    /// Add a search path for plugin assemblies.
    /// </summary>
    public void AddSearchPath(string path)
    {
        if (!_searchPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _searchPaths.Add(path);
        }
    }

    /// <summary>
    /// Load a plugin from an assembly file.
    /// </summary>
    public PluginInfo? LoadPlugin(string assemblyPath, Dictionary<string, object>? configuration = null)
    {
        try
        {
            var resolvedPath = ResolvePath(assemblyPath);
            if (!File.Exists(resolvedPath))
            {
                return new PluginInfo
                {
                    Metadata = new RuleProviderMetadata
                    {
                        ProviderId = Path.GetFileNameWithoutExtension(assemblyPath),
                        Name = Path.GetFileNameWithoutExtension(assemblyPath)
                    },
                    Provider = null!,
                    AssemblyPath = assemblyPath,
                    IsLoaded = false,
                    ErrorMessage = $"Assembly not found: {resolvedPath}"
                };
            }

            if (!_loadedAssemblies.TryGetValue(resolvedPath, out var assembly))
            {
                assembly = Assembly.LoadFrom(resolvedPath);
                _loadedAssemblies[resolvedPath] = assembly;
            }

            // Find IRuleProvider implementations
            var providerTypes = assembly.GetTypes()
                .Where(t => typeof(IRuleProvider).IsAssignableFrom(t)
                           && !t.IsAbstract
                           && !t.IsInterface
                           && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var providerType in providerTypes)
            {
                var provider = (IRuleProvider?)Activator.CreateInstance(providerType);
                if (provider == null) continue;

                provider.Initialize(configuration);

                var pluginInfo = new PluginInfo
                {
                    Metadata = provider.Metadata,
                    Provider = provider,
                    AssemblyPath = resolvedPath,
                    RuleCount = provider.GetRuleAnalyzers().Count() + provider.GetCrossLanguageRules().Count(),
                    IsLoaded = true
                };

                _loadedPlugins[provider.Metadata.ProviderId] = pluginInfo;
                return pluginInfo;
            }

            // No providers found - look for individual rule analyzers
            var analyzerCount = CountAnalyzers(assembly);
            if (analyzerCount > 0)
            {
                var providerId = Path.GetFileNameWithoutExtension(assemblyPath);
                var pluginInfo = new PluginInfo
                {
                    Metadata = new RuleProviderMetadata
                    {
                        ProviderId = providerId,
                        Name = providerId,
                        Description = $"Auto-discovered rules from {Path.GetFileName(assemblyPath)}"
                    },
                    Provider = new AutoDiscoveredRuleProvider(assembly),
                    AssemblyPath = resolvedPath,
                    RuleCount = analyzerCount,
                    IsLoaded = true
                };

                _loadedPlugins[providerId] = pluginInfo;
                return pluginInfo;
            }

            return null;
        }
        catch (Exception ex)
        {
            return new PluginInfo
            {
                Metadata = new RuleProviderMetadata
                {
                    ProviderId = Path.GetFileNameWithoutExtension(assemblyPath),
                    Name = Path.GetFileNameWithoutExtension(assemblyPath)
                },
                Provider = null!,
                AssemblyPath = assemblyPath,
                IsLoaded = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Load all plugins from a directory.
    /// </summary>
    public IEnumerable<PluginInfo> LoadPluginsFromDirectory(string directoryPath, string pattern = "*.dll", Dictionary<string, object>? configuration = null)
    {
        if (!Directory.Exists(directoryPath))
            yield break;

        foreach (var file in Directory.GetFiles(directoryPath, pattern))
        {
            var pluginInfo = LoadPlugin(file, configuration);
            if (pluginInfo != null)
            {
                yield return pluginInfo;
            }
        }
    }

    /// <summary>
    /// Get a loaded plugin by ID.
    /// </summary>
    public PluginInfo? GetPlugin(string providerId)
    {
        return _loadedPlugins.TryGetValue(providerId, out var info) ? info : null;
    }

    /// <summary>
    /// Register all rules from loaded plugins with a rule registry.
    /// </summary>
    public int RegisterRulesWithRegistry(RuleRegistry registry)
    {
        var count = 0;

        foreach (var plugin in _loadedPlugins.Values.Where(p => p.IsLoaded))
        {
            // Register C#-specific analyzers
            foreach (var analyzer in plugin.Provider.GetRuleAnalyzers())
            {
                registry.RegisterType(analyzer.GetType());
                count++;
            }

            // Register cross-language rules
            foreach (var rule in plugin.Provider.GetCrossLanguageRules())
            {
                registry.RegisterCrossLanguageType(rule.GetType());
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Unload a plugin by ID.
    /// </summary>
    public bool UnloadPlugin(string providerId)
    {
        return _loadedPlugins.Remove(providerId);
    }

    /// <summary>
    /// Unload all plugins.
    /// </summary>
    public void UnloadAll()
    {
        _loadedPlugins.Clear();
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path) && File.Exists(path))
            return path;

        // Try current directory
        var resolved = Path.GetFullPath(path);
        if (File.Exists(resolved))
            return resolved;

        // Try search paths
        foreach (var searchPath in _searchPaths)
        {
            resolved = Path.Combine(searchPath, path);
            if (File.Exists(resolved))
                return resolved;

            // Also try with just the filename
            resolved = Path.Combine(searchPath, Path.GetFileName(path));
            if (File.Exists(resolved))
                return resolved;
        }

        // Try plugins subdirectory
        resolved = Path.Combine(Environment.CurrentDirectory, "plugins", path);
        if (File.Exists(resolved))
            return resolved;

        return path;
    }

    private static int CountAnalyzers(Assembly assembly)
    {
        var count = 0;

        // Count IRuleAnalyzer implementations
        count += assembly.GetTypes()
            .Count(t => typeof(IRuleAnalyzer).IsAssignableFrom(t)
                       && !t.IsAbstract
                       && !t.IsInterface);

        // Count ICrossLanguageRule implementations
        count += assembly.GetTypes()
            .Count(t => typeof(ICrossLanguageRule).IsAssignableFrom(t)
                       && !t.IsAbstract
                       && !t.IsInterface);

        return count;
    }
}

/// <summary>
/// Auto-generated rule provider for assemblies without an explicit IRuleProvider.
/// </summary>
internal class AutoDiscoveredRuleProvider : IRuleProvider
{
    private readonly Assembly _assembly;
    private readonly RuleProviderMetadata _metadata;

    public AutoDiscoveredRuleProvider(Assembly assembly)
    {
        _assembly = assembly;
        _metadata = new RuleProviderMetadata
        {
            ProviderId = assembly.GetName().Name ?? "unknown",
            Name = assembly.GetName().Name ?? "Unknown",
            Description = $"Auto-discovered rules from {assembly.GetName().Name}",
            Version = assembly.GetName().Version?.ToString() ?? "1.0.0"
        };
    }

    public RuleProviderMetadata Metadata => _metadata;

    public void Initialize(Dictionary<string, object>? configuration)
    {
        // No initialization needed
    }

    public IEnumerable<IRuleAnalyzer> GetRuleAnalyzers()
    {
        var analyzerTypes = _assembly.GetTypes()
            .Where(t => typeof(IRuleAnalyzer).IsAssignableFrom(t)
                       && !t.IsAbstract
                       && !t.IsInterface
                       && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in analyzerTypes)
        {
            IRuleAnalyzer? analyzer = null;
            try
            {
                analyzer = (IRuleAnalyzer?)Activator.CreateInstance(type);
            }
            catch
            {
                // Skip types that fail to instantiate
            }

            if (analyzer != null)
                yield return analyzer;
        }
    }

    public IEnumerable<ICrossLanguageRule> GetCrossLanguageRules()
    {
        var ruleTypes = _assembly.GetTypes()
            .Where(t => typeof(ICrossLanguageRule).IsAssignableFrom(t)
                       && !t.IsAbstract
                       && !t.IsInterface
                       && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in ruleTypes)
        {
            ICrossLanguageRule? rule = null;
            try
            {
                rule = (ICrossLanguageRule?)Activator.CreateInstance(type);
            }
            catch
            {
                // Skip types that fail to instantiate
            }

            if (rule != null)
                yield return rule;
        }
    }
}
