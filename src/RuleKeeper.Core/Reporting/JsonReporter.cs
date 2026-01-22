using System.Text.Json;
using System.Text.Json.Serialization;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Reporting;

/// <summary>
/// Generates JSON-formatted reports.
/// </summary>
public class JsonReporter : IReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Format => "json";

    public string Generate(AnalysisReport report, OutputConfig config)
    {
        var jsonReport = new JsonReport
        {
            Version = "1.0",
            Tool = new ToolInfo
            {
                Name = "RuleKeeper",
                Version = GetVersion()
            },
            Analysis = new AnalysisInfo
            {
                Path = report.AnalyzedPath,
                ConfigFile = report.ConfigFilePath,
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                Duration = report.Duration.TotalMilliseconds
            },
            Summary = new SummaryInfo
            {
                TotalFiles = report.FileCount,
                TotalViolations = report.ViolationCount,
                BySeverity = new SeverityCounts
                {
                    Critical = report.ViolationsBySeverity.GetValueOrDefault(SeverityLevel.Critical)?.Count ?? 0,
                    High = report.ViolationsBySeverity.GetValueOrDefault(SeverityLevel.High)?.Count ?? 0,
                    Medium = report.ViolationsBySeverity.GetValueOrDefault(SeverityLevel.Medium)?.Count ?? 0,
                    Low = report.ViolationsBySeverity.GetValueOrDefault(SeverityLevel.Low)?.Count ?? 0,
                    Info = report.ViolationsBySeverity.GetValueOrDefault(SeverityLevel.Info)?.Count ?? 0
                },
                ErrorCount = report.Errors.Count
            },
            Violations = report.Violations.Select(v => new ViolationInfo
            {
                RuleId = v.RuleId,
                RuleName = v.RuleName,
                Severity = v.Severity,
                Message = v.Message,
                File = v.FilePath,
                StartLine = v.StartLine,
                StartColumn = v.StartColumn,
                EndLine = v.EndLine,
                EndColumn = v.EndColumn,
                CodeSnippet = config.ShowCode ? v.CodeSnippet : null,
                FixHint = config.ShowHints ? v.FixHint : null
            }).ToList(),
            Errors = report.Errors.Select(e => new ErrorInfo
            {
                File = e.FilePath,
                Message = e.Message
            }).ToList()
        };

        return JsonSerializer.Serialize(jsonReport, JsonOptions);
    }

    public async Task WriteToFileAsync(AnalysisReport report, OutputConfig config, string filePath)
    {
        var content = Generate(report, config);
        await File.WriteAllTextAsync(filePath, content);
    }

    private static string GetVersion()
    {
        var assembly = typeof(JsonReporter).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }
}

#region JSON Models

internal class JsonReport
{
    public required string Version { get; set; }
    public required ToolInfo Tool { get; set; }
    public required AnalysisInfo Analysis { get; set; }
    public required SummaryInfo Summary { get; set; }
    public required List<ViolationInfo> Violations { get; set; }
    public required List<ErrorInfo> Errors { get; set; }
}

internal class ToolInfo
{
    public required string Name { get; set; }
    public required string Version { get; set; }
}

internal class AnalysisInfo
{
    public required string Path { get; set; }
    public string? ConfigFile { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double Duration { get; set; }
}

internal class SummaryInfo
{
    public int TotalFiles { get; set; }
    public int TotalViolations { get; set; }
    public required SeverityCounts BySeverity { get; set; }
    public int ErrorCount { get; set; }
}

internal class SeverityCounts
{
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
    public int Info { get; set; }
}

internal class ViolationInfo
{
    public required string RuleId { get; set; }
    public required string RuleName { get; set; }
    public SeverityLevel Severity { get; set; }
    public required string Message { get; set; }
    public required string File { get; set; }
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string? CodeSnippet { get; set; }
    public string? FixHint { get; set; }
}

internal class ErrorInfo
{
    public required string File { get; set; }
    public required string Message { get; set; }
}

#endregion
