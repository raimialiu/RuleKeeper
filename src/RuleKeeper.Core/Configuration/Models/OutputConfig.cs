using RuleKeeper.Sdk;
using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for output and reporting.
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// Output format (console, json, sarif, html).
    /// </summary>
    [YamlMember(Alias = "format")]
    public string Format { get; set; } = "console";

    /// <summary>
    /// Additional output formats to generate alongside the primary format.
    /// </summary>
    [YamlMember(Alias = "additional_formats")]
    public List<string> AdditionalFormats { get; set; } = new();

    /// <summary>
    /// Output file path (for non-console formats).
    /// </summary>
    [YamlMember(Alias = "file")]
    public string? File { get; set; }

    /// <summary>
    /// Minimum severity level to report.
    /// </summary>
    [YamlMember(Alias = "min_severity")]
    public SeverityLevel MinSeverity { get; set; } = SeverityLevel.Info;

    /// <summary>
    /// Severity level that causes non-zero exit code.
    /// </summary>
    [YamlMember(Alias = "fail_on")]
    public SeverityLevel? FailOn { get; set; }

    /// <summary>
    /// Threshold percentage for total violations (0-100). If violation percentage exceeds this, exit with failure.
    /// Default is 0, meaning any violation causes failure when combined with fail_on.
    /// </summary>
    [YamlMember(Alias = "threshold_percentage")]
    public double ThresholdPercentage { get; set; } = 0;

    /// <summary>
    /// Maximum allowed count of critical violations before failure.
    /// Default is 0, meaning any critical violation causes failure.
    /// </summary>
    [YamlMember(Alias = "critical_threshold")]
    public int CriticalThreshold { get; set; } = 0;

    /// <summary>
    /// Maximum allowed count of high severity violations before failure.
    /// Default is 0, meaning any high severity violation causes failure when fail_on is High or below.
    /// </summary>
    [YamlMember(Alias = "high_threshold")]
    public int HighThreshold { get; set; } = 0;

    /// <summary>
    /// Maximum allowed total violation count before failure.
    /// Default is 0, meaning any violation causes failure when fail_on is set.
    /// </summary>
    [YamlMember(Alias = "total_threshold")]
    public int TotalThreshold { get; set; } = 0;

    /// <summary>
    /// Whether to show code snippets in output.
    /// </summary>
    [YamlMember(Alias = "show_code")]
    public bool ShowCode { get; set; } = true;

    /// <summary>
    /// Whether to show fix hints in output.
    /// </summary>
    [YamlMember(Alias = "show_hints")]
    public bool ShowHints { get; set; } = true;

    /// <summary>
    /// Whether to use colors in console output.
    /// </summary>
    [YamlMember(Alias = "colors")]
    public bool Colors { get; set; } = true;

    /// <summary>
    /// Whether to show verbose output.
    /// </summary>
    [YamlMember(Alias = "verbose")]
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Whether to show visualization charts in reports.
    /// </summary>
    [YamlMember(Alias = "visualization")]
    public bool Visualization { get; set; } = true;

    /// <summary>
    /// Whether to show summary table in console output.
    /// </summary>
    [YamlMember(Alias = "show_table")]
    public bool ShowTable { get; set; } = true;

    /// <summary>
    /// Maximum number of violations to report per rule.
    /// </summary>
    [YamlMember(Alias = "max_per_rule")]
    public int? MaxPerRule { get; set; }

    /// <summary>
    /// Maximum total violations to report.
    /// </summary>
    [YamlMember(Alias = "max_total")]
    public int? MaxTotal { get; set; }

    /// <summary>
    /// Show only summary tables without individual violation details.
    /// </summary>
    [YamlMember(Alias = "summary_only")]
    public bool SummaryOnly { get; set; } = false;
}
