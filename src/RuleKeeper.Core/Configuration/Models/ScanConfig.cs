using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Supported programming languages for analysis.
/// </summary>
public enum Language
{
    CSharp,
    // Future support (not yet implemented)
    // FSharp,
    // VisualBasic,
    // TypeScript,
    // Java
}

/// <summary>
/// Configuration for scan behavior.
/// </summary>
public class ScanConfig
{
    /// <summary>
    /// Programming language to analyze. Defaults to C#.
    /// Currently only C# is supported; other languages may be added in future versions.
    /// </summary>
    [YamlMember(Alias = "language")]
    public Language Language { get; set; } = Language.CSharp;

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
        return language switch
        {
            Language.CSharp => new List<string> { "**/*.cs" },
            // Future language support
            // Language.FSharp => new List<string> { "**/*.fs", "**/*.fsx" },
            // Language.VisualBasic => new List<string> { "**/*.vb" },
            // Language.TypeScript => new List<string> { "**/*.ts", "**/*.tsx" },
            // Language.Java => new List<string> { "**/*.java" },
            _ => new List<string> { "**/*.cs" }
        };
    }
}
