using System.Collections.Concurrent;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Languages;

/// <summary>
/// Registry for language adapters that provides parsing capabilities for different programming languages.
/// </summary>
public class LanguageAdapterRegistry
{
    private readonly ConcurrentDictionary<Language, ILanguageAdapter> _adapters = new();
    private readonly ConcurrentDictionary<string, Language> _extensionToLanguage = new();

    /// <summary>
    /// Gets the singleton instance of the registry.
    /// </summary>
    public static LanguageAdapterRegistry Instance { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageAdapterRegistry"/> class.
    /// </summary>
    public LanguageAdapterRegistry()
    {
    }

    /// <summary>
    /// Registers a language adapter.
    /// </summary>
    /// <param name="adapter">The adapter to register.</param>
    public void RegisterAdapter(ILanguageAdapter adapter)
    {
        _adapters[adapter.Language] = adapter;

        foreach (var extension in adapter.SupportedExtensions)
        {
            var normalizedExtension = extension.StartsWith(".") ? extension : $".{extension}";
            _extensionToLanguage[normalizedExtension.ToLowerInvariant()] = adapter.Language;
        }
    }

    /// <summary>
    /// Gets the adapter for a specific language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The adapter, or null if not registered.</returns>
    public ILanguageAdapter? GetAdapter(Language language)
    {
        return _adapters.TryGetValue(language, out var adapter) ? adapter : null;
    }

    /// <summary>
    /// Gets the adapter for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The adapter, or null if no adapter supports the file type.</returns>
    public ILanguageAdapter? GetAdapterForFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return null;

        if (_extensionToLanguage.TryGetValue(extension, out var language))
        {
            return GetAdapter(language);
        }

        return null;
    }

    /// <summary>
    /// Gets the language for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The language, or null if unknown.</returns>
    public Language? GetLanguageForFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return null;

        return _extensionToLanguage.TryGetValue(extension, out var language) ? language : null;
    }

    /// <summary>
    /// Determines if a language is supported.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>True if an adapter is registered for the language.</returns>
    public bool IsLanguageSupported(Language language)
    {
        return _adapters.ContainsKey(language);
    }

    /// <summary>
    /// Determines if a file type is supported.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>True if an adapter can handle the file.</returns>
    public bool IsFileSupported(string filePath)
    {
        return GetAdapterForFile(filePath) != null;
    }

    /// <summary>
    /// Gets all registered adapters.
    /// </summary>
    public IEnumerable<ILanguageAdapter> GetAllAdapters()
    {
        return _adapters.Values;
    }

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    public IEnumerable<Language> GetSupportedLanguages()
    {
        return _adapters.Keys;
    }

    /// <summary>
    /// Gets all supported file extensions.
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return _extensionToLanguage.Keys;
    }

    /// <summary>
    /// Removes an adapter from the registry.
    /// </summary>
    /// <param name="language">The language to remove.</param>
    /// <returns>True if the adapter was removed.</returns>
    public bool RemoveAdapter(Language language)
    {
        if (_adapters.TryRemove(language, out var adapter))
        {
            foreach (var extension in adapter.SupportedExtensions)
            {
                var normalizedExtension = extension.StartsWith(".") ? extension : $".{extension}";
                _extensionToLanguage.TryRemove(normalizedExtension.ToLowerInvariant(), out _);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all registered adapters.
    /// </summary>
    public void Clear()
    {
        _adapters.Clear();
        _extensionToLanguage.Clear();
    }

    /// <summary>
    /// Parses a file using the appropriate adapter.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed syntax tree root, or null if no adapter supports the file.</returns>
    public async Task<IUnifiedSyntaxNode?> ParseFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var adapter = GetAdapterForFile(filePath);
        if (adapter == null)
            return null;

        var source = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await adapter.ParseAsync(source, filePath, cancellationToken);
    }

    /// <summary>
    /// Parses source code using a specific language adapter.
    /// </summary>
    /// <param name="source">The source code.</param>
    /// <param name="filePath">The file path (for error reporting).</param>
    /// <param name="language">The language to use for parsing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed syntax tree root, or null if the language is not supported.</returns>
    public async Task<IUnifiedSyntaxNode?> ParseAsync(
        string source,
        string filePath,
        Language language,
        CancellationToken cancellationToken = default)
    {
        var adapter = GetAdapter(language);
        if (adapter == null)
            return null;

        return await adapter.ParseAsync(source, filePath, cancellationToken);
    }
}
