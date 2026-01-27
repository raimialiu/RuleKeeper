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
    /// Baseline configuration for incremental scanning.
    /// When enabled, only new or changed code is reported.
    /// </summary>
    [YamlMember(Alias = "baseline")]
    public BaselineConfig? Baseline { get; set; }

    /// <summary>
    /// Returns true if baseline scanning is enabled.
    /// </summary>
    public bool IsBaselineEnabled => Baseline?.Enabled == true;

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
/// Configuration for baseline/incremental scanning.
/// Allows scanning only new or changed code to avoid breaking existing projects.
/// </summary>
public class BaselineConfig
{
    /// <summary>
    /// Whether baseline scanning is enabled.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Baseline mode: git, file, date, or legacy_files.
    /// - git: Compare against a git commit, branch, or tag
    /// - file: Use a baseline file storing previous violations
    /// - date: Only scan files modified after a specific date
    /// - legacy_files: Skip files captured at baseline creation (for legacy code adoption)
    /// </summary>
    [YamlMember(Alias = "mode")]
    public string Mode { get; set; } = "git";

    /// <summary>
    /// Git reference to compare against (branch, tag, or commit hash).
    /// Examples: "main", "origin/main", "v1.0.0", "abc123"
    /// </summary>
    [YamlMember(Alias = "git_ref")]
    public string? GitRef { get; set; }

    /// <summary>
    /// Only scan files changed in git (not the entire file, just changed lines).
    /// When false, scans entire files that have any changes.
    /// </summary>
    [YamlMember(Alias = "changed_lines_only")]
    public bool ChangedLinesOnly { get; set; } = false;

    /// <summary>
    /// Include uncommitted changes (staged and unstaged).
    /// </summary>
    [YamlMember(Alias = "include_uncommitted")]
    public bool IncludeUncommitted { get; set; } = true;

    /// <summary>
    /// Include untracked (new) files.
    /// </summary>
    [YamlMember(Alias = "include_untracked")]
    public bool IncludeUntracked { get; set; } = true;

    /// <summary>
    /// Path to baseline file (when mode = file).
    /// Stores violations from a previous run for comparison.
    /// </summary>
    [YamlMember(Alias = "baseline_file")]
    public string? BaselineFile { get; set; }

    /// <summary>
    /// Date to compare against (when mode = date).
    /// Only files modified after this date are scanned.
    /// Format: YYYY-MM-DD or ISO 8601
    /// </summary>
    [YamlMember(Alias = "since_date")]
    public string? SinceDate { get; set; }

    /// <summary>
    /// Action to take when baseline is outdated or missing.
    /// - warn: Show warning but continue scanning all files
    /// - fail: Exit with error
    /// - ignore: Silently scan all files
    /// </summary>
    [YamlMember(Alias = "on_missing")]
    public string OnMissing { get; set; } = "warn";

    /// <summary>
    /// Whether to update the baseline file after scanning.
    /// Only applicable when mode = file.
    /// </summary>
    [YamlMember(Alias = "auto_update")]
    public bool AutoUpdate { get; set; } = false;

    /// <summary>
    /// Filter violations to only those on changed lines (post-processing).
    /// This is applied after scanning to filter results.
    /// </summary>
    [YamlMember(Alias = "filter_to_diff")]
    public bool FilterToDiff { get; set; } = true;

    /// <summary>
    /// When using legacy_files mode, track file modifications.
    /// If true, files that have been modified since baseline creation will be scanned.
    /// If false, legacy files are always skipped regardless of modifications.
    /// </summary>
    [YamlMember(Alias = "track_modifications")]
    public bool TrackModifications { get; set; } = true;
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
