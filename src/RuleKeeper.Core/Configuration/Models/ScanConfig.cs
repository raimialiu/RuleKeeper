using RuleKeeper.Sdk;
using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for scan behavior.
/// </summary>
public class ScanConfig
{
    /// <summary>
    /// Programming languages to analyze. Defaults to C#.
    /// Multiple languages can be specified for multi-language analysis.
    /// </summary>
    [YamlMember(Alias = "languages")]
    public List<Language> Languages { get; set; } = new() { Language.CSharp };

    /// <summary>
    /// Programming language to analyze. Defaults to C#.
    /// This is kept for backward compatibility. Use <see cref="Languages"/> for multi-language support.
    /// </summary>
    [YamlMember(Alias = "language")]
    public Language Language
    {
        get => Languages.FirstOrDefault();
        set
        {
            if (Languages.Count == 0 || (Languages.Count == 1 && Languages[0] == Language.CSharp))
            {
                Languages = new List<Language> { value };
            }
        }
    }

    /// <summary>
    /// Language-specific settings for multi-language analysis.
    /// </summary>
    [YamlMember(Alias = "language_settings")]
    public Dictionary<string, LanguageSettings> LanguageSettings { get; set; } = new();

    /// <summary>
    /// File patterns to include in the scan.
    /// If not specified, defaults based on the selected language.
    /// </summary>
    [YamlMember(Alias = "include")]
    public List<string> Include { get; set; } = new() { "**/*.cs" };

    /// <summary>
    /// File patterns to exclude from the scan.
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new()
    {
        "**/obj/**",
        "**/bin/**",
        "**/*.Designer.cs",
        "**/*.g.cs",
        "**/*.generated.cs"
    };

    /// <summary>
    /// Whether to enable parallel analysis.
    /// </summary>
    [YamlMember(Alias = "parallel")]
    public bool Parallel { get; set; } = true;

    /// <summary>
    /// Maximum degree of parallelism.
    /// </summary>
    [YamlMember(Alias = "max_parallelism")]
    public int MaxParallelism { get; set; } = 0; // 0 = use system default

    /// <summary>
    /// Whether to enable caching.
    /// </summary>
    [YamlMember(Alias = "cache")]
    public bool Cache { get; set; } = true;

    /// <summary>
    /// Path to cache directory.
    /// </summary>
    [YamlMember(Alias = "cache_path")]
    public string? CachePath { get; set; }

    /// <summary>
    /// Whether to follow symbolic links.
    /// </summary>
    [YamlMember(Alias = "follow_symlinks")]
    public bool FollowSymlinks { get; set; } = false;

    /// <summary>
    /// Maximum file size to analyze in bytes.
    /// </summary>
    [YamlMember(Alias = "max_file_size")]
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Gets the default file patterns for the configured language.
    /// </summary>
    public static List<string> GetDefaultIncludePatterns(Language language)
    {
        return language.GetDefaultIncludePatterns().ToList();
    }

    /// <summary>
    /// Gets the default exclude patterns for the configured language.
    /// </summary>
    public static List<string> GetDefaultExcludePatterns(Language language)
    {
        return language.GetDefaultExcludePatterns().ToList();
    }

    /// <summary>
    /// Gets all include patterns for all configured languages.
    /// </summary>
    public List<string> GetAllIncludePatterns()
    {
        var patterns = new HashSet<string>(Include);

        foreach (var lang in Languages)
        {
            if (LanguageSettings.TryGetValue(lang.ToString().ToLowerInvariant(), out var settings))
            {
                foreach (var pattern in settings.Include)
                {
                    patterns.Add(pattern);
                }
            }
            else
            {
                foreach (var pattern in lang.GetDefaultIncludePatterns())
                {
                    patterns.Add(pattern);
                }
            }
        }

        return patterns.ToList();
    }

    /// <summary>
    /// Gets all exclude patterns for all configured languages.
    /// </summary>
    public List<string> GetAllExcludePatterns()
    {
        var patterns = new HashSet<string>(Exclude);

        foreach (var lang in Languages)
        {
            if (LanguageSettings.TryGetValue(lang.ToString().ToLowerInvariant(), out var settings))
            {
                foreach (var pattern in settings.Exclude)
                {
                    patterns.Add(pattern);
                }
            }
            else
            {
                foreach (var pattern in lang.GetDefaultExcludePatterns())
                {
                    patterns.Add(pattern);
                }
            }
        }

        return patterns.ToList();
    }
}

/// <summary>
/// Language-specific scan settings.
/// </summary>
public class LanguageSettings
{
    /// <summary>
    /// File patterns to include for this language.
    /// </summary>
    [YamlMember(Alias = "include")]
    public List<string> Include { get; set; } = new();

    /// <summary>
    /// File patterns to exclude for this language.
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Whether to use project files for analysis (e.g., .csproj, package.json).
    /// </summary>
    [YamlMember(Alias = "use_project_files")]
    public bool UseProjectFiles { get; set; } = true;

    /// <summary>
    /// Path to TypeScript configuration file (for TypeScript projects).
    /// </summary>
    [YamlMember(Alias = "tsconfig_path")]
    public string? TsConfigPath { get; set; }

    /// <summary>
    /// Additional configuration options for the language adapter.
    /// </summary>
    [YamlMember(Alias = "options")]
    public Dictionary<string, object> Options { get; set; } = new();
}
