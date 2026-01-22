using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Analysis;

/// <summary>
/// Contains the results of a code analysis run.
/// </summary>
public record AnalysisReport
{
    /// <summary>
    /// When the analysis started.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the analysis completed.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Duration of the analysis.
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

    /// <summary>
    /// The path that was analyzed.
    /// </summary>
    public required string AnalyzedPath { get; init; }

    /// <summary>
    /// All violations found during analysis.
    /// </summary>
    public List<Violation> Violations { get; set; } = new();

    /// <summary>
    /// Files that were analyzed.
    /// </summary>
    public List<string> AnalyzedFiles { get; init; } = new();

    /// <summary>
    /// Files that were skipped.
    /// </summary>
    public List<SkippedFile> SkippedFiles { get; init; } = new();

    /// <summary>
    /// Any errors that occurred during analysis.
    /// </summary>
    public List<AnalysisError> Errors { get; init; } = new();

    /// <summary>
    /// Configuration file path used.
    /// </summary>
    public string? ConfigFilePath { get; init; }

    /// <summary>
    /// Total number of files analyzed.
    /// </summary>
    public int FileCount => AnalyzedFiles.Count;

    /// <summary>
    /// Total number of violations.
    /// </summary>
    public int ViolationCount => Violations.Count;

    /// <summary>
    /// Gets violations grouped by severity.
    /// </summary>
    public Dictionary<SeverityLevel, List<Violation>> ViolationsBySeverity =>
        Violations.GroupBy(v => v.Severity)
            .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// Gets violations grouped by rule ID.
    /// </summary>
    public Dictionary<string, List<Violation>> ViolationsByRule =>
        Violations.GroupBy(v => v.RuleId)
            .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// Gets violations grouped by file.
    /// </summary>
    public Dictionary<string, List<Violation>> ViolationsByFile =>
        Violations.GroupBy(v => v.FilePath)
            .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// Checks if any violations meet or exceed the specified severity.
    /// </summary>
    public bool HasViolationsAtOrAbove(SeverityLevel severity)
    {
        return Violations.Any(v => v.Severity >= severity);
    }

    /// <summary>
    /// Gets the highest severity level found.
    /// </summary>
    public SeverityLevel? HighestSeverity =>
        Violations.Count > 0 ? Violations.Max(v => v.Severity) : null;

    /// <summary>
    /// Summary statistics for the analysis.
    /// </summary>
    public AnalysisSummary GetSummary()
    {
        var bySeverity = ViolationsBySeverity;
        return new AnalysisSummary
        {
            TotalFiles = FileCount,
            TotalViolations = ViolationCount,
            CriticalCount = bySeverity.GetValueOrDefault(SeverityLevel.Critical)?.Count ?? 0,
            HighCount = bySeverity.GetValueOrDefault(SeverityLevel.High)?.Count ?? 0,
            MediumCount = bySeverity.GetValueOrDefault(SeverityLevel.Medium)?.Count ?? 0,
            LowCount = bySeverity.GetValueOrDefault(SeverityLevel.Low)?.Count ?? 0,
            InfoCount = bySeverity.GetValueOrDefault(SeverityLevel.Info)?.Count ?? 0,
            Duration = Duration,
            ErrorCount = Errors.Count
        };
    }
}

/// <summary>
/// Summary statistics for an analysis run.
/// </summary>
public class AnalysisSummary
{
    public int TotalFiles { get; init; }
    public int TotalViolations { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public int InfoCount { get; init; }
    public TimeSpan Duration { get; init; }
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the percentage of files with violations.
    /// </summary>
    public double ViolationPercentage => TotalFiles > 0 ? (double)TotalViolations / TotalFiles * 100 : 0;

    /// <summary>
    /// Checks if violations exceed the configured thresholds.
    /// </summary>
    public bool HasFailures(SeverityLevel failOn)
    {
        return failOn switch
        {
            SeverityLevel.Critical => CriticalCount > 0,
            SeverityLevel.High => CriticalCount > 0 || HighCount > 0,
            SeverityLevel.Medium => CriticalCount > 0 || HighCount > 0 || MediumCount > 0,
            SeverityLevel.Low => CriticalCount > 0 || HighCount > 0 || MediumCount > 0 || LowCount > 0,
            SeverityLevel.Info => TotalViolations > 0,
            _ => false
        };
    }

    /// <summary>
    /// Checks if violations exceed the configured thresholds.
    /// </summary>
    /// <param name="failOn">The severity level to fail on</param>
    /// <param name="criticalThreshold">Maximum allowed critical violations (default 0)</param>
    /// <param name="highThreshold">Maximum allowed high severity violations (default 0)</param>
    /// <param name="totalThreshold">Maximum allowed total violations (default 0)</param>
    /// <param name="thresholdPercentage">Maximum allowed violation percentage (default 0)</param>
    /// <returns>True if any threshold is exceeded</returns>
    public bool ExceedsThresholds(
        SeverityLevel? failOn = null,
        int criticalThreshold = 0,
        int highThreshold = 0,
        int totalThreshold = 0,
        double thresholdPercentage = 0)
    {
        // Check critical threshold
        if (CriticalCount > criticalThreshold)
            return true;

        // Check high threshold if fail_on is High or below
        if (failOn.HasValue && failOn.Value <= SeverityLevel.High && HighCount > highThreshold)
            return true;

        // Check total threshold
        if (totalThreshold > 0 && TotalViolations > totalThreshold)
            return true;

        // Check percentage threshold
        if (thresholdPercentage > 0 && ViolationPercentage > thresholdPercentage)
            return true;

        // Standard fail_on check if thresholds are all zero (default behavior)
        if (failOn.HasValue && criticalThreshold == 0 && highThreshold == 0 && totalThreshold == 0 && thresholdPercentage == 0)
        {
            return HasFailures(failOn.Value);
        }

        return false;
    }

    /// <summary>
    /// Gets a human-readable pass/fail status based on thresholds.
    /// </summary>
    public string GetStatus(
        SeverityLevel? failOn = null,
        int criticalThreshold = 0,
        int highThreshold = 0,
        int totalThreshold = 0,
        double thresholdPercentage = 0)
    {
        if (ExceedsThresholds(failOn, criticalThreshold, highThreshold, totalThreshold, thresholdPercentage))
        {
            if (CriticalCount > criticalThreshold)
                return $"FAILED: {CriticalCount} critical violation(s) exceeded threshold of {criticalThreshold}";
            if (failOn.HasValue && failOn.Value <= SeverityLevel.High && HighCount > highThreshold)
                return $"FAILED: {HighCount} high severity violation(s) exceeded threshold of {highThreshold}";
            if (totalThreshold > 0 && TotalViolations > totalThreshold)
                return $"FAILED: {TotalViolations} total violation(s) exceeded threshold of {totalThreshold}";
            if (thresholdPercentage > 0 && ViolationPercentage > thresholdPercentage)
                return $"FAILED: {ViolationPercentage:F1}% violation rate exceeded threshold of {thresholdPercentage}%";
            return "FAILED: Violations found at or above configured severity";
        }

        if (TotalViolations == 0)
            return "PASSED: No violations found";

        return $"PASSED: {TotalViolations} violation(s) within acceptable thresholds";
    }
}

/// <summary>
/// Information about a skipped file.
/// </summary>
public class SkippedFile
{
    public required string FilePath { get; init; }
    public required string Reason { get; init; }
}

/// <summary>
/// An error that occurred during analysis.
/// </summary>
public class AnalysisError
{
    public required string FilePath { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
}
