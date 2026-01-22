using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Fixing;

/// <summary>
/// Result of applying a code fix.
/// </summary>
public class FixResult
{
    /// <summary>
    /// The fix that was applied.
    /// </summary>
    public CodeFix Fix { get; set; } = null!;

    /// <summary>
    /// Whether the fix was successfully applied.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the fix failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The file path that was modified.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    public static FixResult Succeeded(CodeFix fix, string filePath) => new()
    {
        Fix = fix,
        Success = true,
        FilePath = filePath
    };

    public static FixResult Failed(CodeFix fix, string filePath, string error) => new()
    {
        Fix = fix,
        Success = false,
        FilePath = filePath,
        ErrorMessage = error
    };
}

/// <summary>
/// Summary of all fixes applied.
/// </summary>
public class FixSummary
{
    public int TotalFixesAttempted { get; set; }
    public int SuccessfulFixes { get; set; }
    public int FailedFixes { get; set; }
    public int FilesModified { get; set; }
    public List<FixResult> Results { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public Dictionary<string, int> FixesByRule { get; set; } = new();
    public Dictionary<string, int> FixesByCategory { get; set; } = new();
}
