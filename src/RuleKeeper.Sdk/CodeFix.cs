namespace RuleKeeper.Sdk;

/// <summary>
/// Represents a code fix that can be applied to resolve a violation.
/// </summary>
public class CodeFix
{
    /// <summary>
    /// Unique identifier for this fix.
    /// </summary>
    public string FixId { get; set; } = string.Empty;

    /// <summary>
    /// The rule ID this fix addresses.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this fix does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The file path where the fix should be applied.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The type of fix operation.
    /// </summary>
    public FixOperation Operation { get; set; } = FixOperation.Replace;

    /// <summary>
    /// Starting line number (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Starting column (1-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Ending line number (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Ending column (1-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// The original text to be replaced.
    /// </summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// The new text to replace with.
    /// </summary>
    public string ReplacementText { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the issue being fixed.
    /// </summary>
    public SeverityLevel Severity { get; set; }

    /// <summary>
    /// Whether this fix is considered safe (won't change behavior).
    /// </summary>
    public bool IsSafe { get; set; } = true;

    /// <summary>
    /// Category of the fix (naming, async, security, etc.).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Creates a simple replacement fix.
    /// </summary>
    public static CodeFix CreateReplacement(
        Violation violation,
        string description,
        int startColumn,
        int endColumn,
        string originalText,
        string replacementText,
        bool isSafe = true)
    {
        return new CodeFix
        {
            FixId = $"FIX-{violation.RuleId}-{Guid.NewGuid():N}",
            RuleId = violation.RuleId,
            Description = description,
            FilePath = violation.FilePath,
            Operation = FixOperation.Replace,
            StartLine = violation.StartLine,
            StartColumn = startColumn,
            EndLine = violation.StartLine,
            EndColumn = endColumn,
            OriginalText = originalText,
            ReplacementText = replacementText,
            Severity = violation.Severity,
            IsSafe = isSafe,
            Category = ExtractCategory(violation.RuleId)
        };
    }

    /// <summary>
    /// Creates an insertion fix.
    /// </summary>
    public static CodeFix CreateInsertion(
        Violation violation,
        string description,
        int line,
        int column,
        string textToInsert,
        bool isSafe = true)
    {
        return new CodeFix
        {
            FixId = $"FIX-{violation.RuleId}-{Guid.NewGuid():N}",
            RuleId = violation.RuleId,
            Description = description,
            FilePath = violation.FilePath,
            Operation = FixOperation.Insert,
            StartLine = line,
            StartColumn = column,
            EndLine = line,
            EndColumn = column,
            OriginalText = "",
            ReplacementText = textToInsert,
            Severity = violation.Severity,
            IsSafe = isSafe,
            Category = ExtractCategory(violation.RuleId)
        };
    }

    private static string ExtractCategory(string ruleId)
    {
        // Extract category from rule ID like "CS-NAME-001" -> "naming"
        var parts = ruleId.Split('-');
        if (parts.Length >= 2)
        {
            return parts[1].ToLowerInvariant() switch
            {
                "name" => "naming",
                "sec" => "security",
                "async" => "async",
                "exc" => "exceptions",
                "design" => "design",
                "di" => "dependency-injection",
                _ => parts[1].ToLowerInvariant()
            };
        }
        return "general";
    }
}

/// <summary>
/// Types of fix operations.
/// </summary>
public enum FixOperation
{
    /// <summary>
    /// Replace text at the specified location.
    /// </summary>
    Replace,

    /// <summary>
    /// Insert text at the specified location.
    /// </summary>
    Insert,

    /// <summary>
    /// Delete text at the specified location.
    /// </summary>
    Delete,

    /// <summary>
    /// Add a using directive.
    /// </summary>
    AddUsing,

    /// <summary>
    /// Remove a using directive.
    /// </summary>
    RemoveUsing
}
