using System.Reflection;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Languages;

/// <summary>
/// Factory for creating and discovering language adapters.
/// </summary>
public static class AdapterFactory
{
    /// <summary>
    /// Discovers and registers adapters from an assembly.
    /// </summary>
    /// <param name="registry">The registry to add adapters to.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The number of adapters registered.</returns>
    public static int DiscoverAdapters(LanguageAdapterRegistry registry, Assembly assembly)
    {
        var count = 0;
        var adapterType = typeof(ILanguageAdapter);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || !adapterType.IsAssignableFrom(type))
                continue;

            try
            {
                var adapter = (ILanguageAdapter?)Activator.CreateInstance(type);
                if (adapter != null)
                {
                    registry.RegisterAdapter(adapter);
                    count++;
                }
            }
            catch
            {
                // Dont know what to do here, so I'll leave this one for now
            }
        }

        return count;
    }

    /// <summary>
    /// Discovers and registers adapters from an assembly file.
    /// </summary>
    /// <param name="registry">The registry to add adapters to.</param>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>The number of adapters registered.</returns>
    public static int DiscoverAdapters(LanguageAdapterRegistry registry, string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        return DiscoverAdapters(registry, assembly);
    }

    /// <summary>
    /// Creates a language adapter of the specified type.
    /// </summary>
    /// <typeparam name="T">The adapter type.</typeparam>
    /// <returns>The adapter instance.</returns>
    public static T CreateAdapter<T>() where T : ILanguageAdapter, new()
    {
        return new T();
    }

    /// <summary>
    /// Creates a language adapter from a type.
    /// </summary>
    /// <param name="adapterType">The adapter type.</param>
    /// <returns>The adapter instance, or null if creation failed.</returns>
    public static ILanguageAdapter? CreateAdapter(Type adapterType)
    {
        if (!typeof(ILanguageAdapter).IsAssignableFrom(adapterType))
            return null;

        return (ILanguageAdapter?)Activator.CreateInstance(adapterType);
    }

    /// <summary>
    /// Gets adapter information without instantiating it.
    /// </summary>
    /// <param name="adapterType">The adapter type.</param>
    /// <returns>Information about the adapter, or null if not a valid adapter type.</returns>
    public static AdapterInfo? GetAdapterInfo(Type adapterType)
    {
        if (!typeof(ILanguageAdapter).IsAssignableFrom(adapterType) ||
            adapterType.IsAbstract ||
            adapterType.IsInterface)
            return null;

        // Try to get info from attributes or by creating an instance
        var languageAttr = adapterType.GetCustomAttribute<LanguageAdapterAttribute>();
        if (languageAttr != null)
        {
            return new AdapterInfo
            {
                Type = adapterType,
                Language = languageAttr.Language,
                Name = languageAttr.Name ?? adapterType.Name,
                Description = languageAttr.Description
            };
        }

        // Create temporary instance to get info
        try
        {
            var adapter = (ILanguageAdapter?)Activator.CreateInstance(adapterType);
            if (adapter != null)
            {
                return new AdapterInfo
                {
                    Type = adapterType,
                    Language = adapter.Language,
                    Name = adapterType.Name,
                    Extensions = adapter.SupportedExtensions.ToList()
                };
            }
        }
        catch
        {
            // Ignore instantiation errors
        }

        return null;
    }

    /// <summary>
    /// Initializes the registry with built-in adapters.
    /// </summary>
    /// <param name="registry">The registry to initialize.</param>
    /// <param name="languages">Optional list of languages to load. If null, loads all available.</param>
    public static void InitializeBuiltInAdapters(LanguageAdapterRegistry registry, IEnumerable<Language>? languages = null)
    {
        // This will be populated as we add language adapter projects
        // For now, just log that we're looking for adapters

        // Try to load from known assembly names
        var assemblyNames = new Dictionary<Language, string>
        {
            { Language.CSharp, "RuleKeeper.Languages.CSharp" },
            { Language.Python, "RuleKeeper.Languages.Python" },
            { Language.JavaScript, "RuleKeeper.Languages.JavaScript" },
            { Language.TypeScript, "RuleKeeper.Languages.JavaScript" }, // JS adapter handles TS too
            { Language.Java, "RuleKeeper.Languages.Java" },
            { Language.Go, "RuleKeeper.Languages.Go" }
        };

        var targetLanguages = languages?.ToHashSet() ?? assemblyNames.Keys.ToHashSet();

        foreach (var (language, assemblyName) in assemblyNames)
        {
            if (!targetLanguages.Contains(language))
                continue;

            try
            {
                var assembly = Assembly.Load(assemblyName);
                DiscoverAdapters(registry, assembly);
            }
            catch
            {
                // Assembly not available, skip
            }
        }
    }
}

/// <summary>
/// Information about a language adapter.
/// </summary>
public class AdapterInfo
{
    /// <summary>
    /// The adapter type.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// The language the adapter handles.
    /// </summary>
    public required Language Language { get; init; }

    /// <summary>
    /// The adapter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Supported file extensions.
    /// </summary>
    public List<string> Extensions { get; init; } = new();
}

/// <summary>
/// Attribute for marking language adapter classes with metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LanguageAdapterAttribute : Attribute
{
    /// <summary>
    /// The language this adapter handles.
    /// </summary>
    public Language Language { get; }

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageAdapterAttribute"/> class.
    /// </summary>
    /// <param name="language">The language this adapter handles.</param>
    public LanguageAdapterAttribute(Language language)
    {
        Language = language;
    }
}
