using System.Text.Json;
using System.Text.Json.Serialization;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Reporting;

/// <summary>
/// Generates SARIF (Static Analysis Results Interchange Format) reports.
/// Useful for CI/CD integration with GitHub, Azure DevOps, etc.
/// </summary>
public class SarifReporter : IReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Format => "sarif";

    public string Generate(AnalysisReport report, OutputConfig config)
    {
        var sarifLog = new SarifLog
        {
            Schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            Version = "2.1.0",
            Runs = new List<SarifRun>
            {
                new SarifRun
                {
                    Tool = new SarifTool
                    {
                        Driver = new SarifDriver
                        {
                            Name = "RuleKeeper",
                            Version = GetVersion(),
                            InformationUri = "https://github.com/rulekeeper/rulekeeper",
                            Rules = GetRuleDescriptors(report)
                        }
                    },
                    Results = report.Violations.Select(v => CreateResult(v, report.AnalyzedPath)).ToList(),
                    Invocations = new List<SarifInvocation>
                    {
                        new SarifInvocation
                        {
                            ExecutionSuccessful = report.Errors.Count == 0,
                            StartTimeUtc = report.StartTime.ToString("O"),
                            EndTimeUtc = report.EndTime?.ToString("O"),
                            WorkingDirectory = new SarifArtifactLocation
                            {
                                Uri = new Uri(report.AnalyzedPath).AbsoluteUri
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(sarifLog, JsonOptions);
    }

    public async Task WriteToFileAsync(AnalysisReport report, OutputConfig config, string filePath)
    {
        var content = Generate(report, config);
        await File.WriteAllTextAsync(filePath, content);
    }

    private static SarifResult CreateResult(Violation violation, string basePath)
    {
        return new SarifResult
        {
            RuleId = violation.RuleId,
            Level = MapSeverityToLevel(violation.Severity),
            Message = new SarifMessage { Text = violation.Message },
            Locations = new List<SarifLocation>
            {
                new SarifLocation
                {
                    PhysicalLocation = new SarifPhysicalLocation
                    {
                        ArtifactLocation = new SarifArtifactLocation
                        {
                            Uri = GetRelativeUri(violation.FilePath, basePath)
                        },
                        Region = new SarifRegion
                        {
                            StartLine = violation.StartLine,
                            StartColumn = violation.StartColumn,
                            EndLine = violation.EndLine,
                            EndColumn = violation.EndColumn,
                            Snippet = !string.IsNullOrEmpty(violation.CodeSnippet)
                                ? new SarifSnippet { Text = violation.CodeSnippet }
                                : null
                        }
                    }
                }
            },
            Fixes = !string.IsNullOrEmpty(violation.FixHint)
                ? new List<SarifFix>
                {
                    new SarifFix
                    {
                        Description = new SarifMessage { Text = violation.FixHint }
                    }
                }
                : null
        };
    }

    private static List<SarifRule> GetRuleDescriptors(AnalysisReport report)
    {
        return report.Violations
            .GroupBy(v => v.RuleId)
            .Select(g => new SarifRule
            {
                Id = g.Key,
                Name = g.First().RuleName,
                DefaultConfiguration = new SarifConfiguration
                {
                    Level = MapSeverityToLevel(g.First().Severity)
                }
            })
            .ToList();
    }

    private static string MapSeverityToLevel(SeverityLevel severity)
    {
        return severity switch
        {
            SeverityLevel.Critical => "error",
            SeverityLevel.High => "error",
            SeverityLevel.Medium => "warning",
            SeverityLevel.Low => "note",
            SeverityLevel.Info => "note",
            _ => "note"
        };
    }

    private static string GetRelativeUri(string fullPath, string basePath)
    {
        try
        {
            var relativePath = Path.GetRelativePath(basePath, fullPath);
            return relativePath.Replace('\\', '/');
        }
        catch
        {
            return fullPath;
        }
    }

    private static string GetVersion()
    {
        var assembly = typeof(SarifReporter).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }
}

#region SARIF Models

internal class SarifLog
{
    [JsonPropertyName("$schema")]
    public required string Schema { get; set; }
    public required string Version { get; set; }
    public required List<SarifRun> Runs { get; set; }
}

internal class SarifRun
{
    public required SarifTool Tool { get; set; }
    public required List<SarifResult> Results { get; set; }
    public List<SarifInvocation>? Invocations { get; set; }
}

internal class SarifTool
{
    public required SarifDriver Driver { get; set; }
}

internal class SarifDriver
{
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? InformationUri { get; set; }
    public List<SarifRule>? Rules { get; set; }
}

internal class SarifRule
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public SarifConfiguration? DefaultConfiguration { get; set; }
}

internal class SarifConfiguration
{
    public string? Level { get; set; }
}

internal class SarifResult
{
    public required string RuleId { get; set; }
    public required string Level { get; set; }
    public required SarifMessage Message { get; set; }
    public List<SarifLocation>? Locations { get; set; }
    public List<SarifFix>? Fixes { get; set; }
}

internal class SarifMessage
{
    public required string Text { get; set; }
}

internal class SarifLocation
{
    public required SarifPhysicalLocation PhysicalLocation { get; set; }
}

internal class SarifPhysicalLocation
{
    public required SarifArtifactLocation ArtifactLocation { get; set; }
    public SarifRegion? Region { get; set; }
}

internal class SarifArtifactLocation
{
    public required string Uri { get; set; }
}

internal class SarifRegion
{
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public SarifSnippet? Snippet { get; set; }
}

internal class SarifSnippet
{
    public required string Text { get; set; }
}

internal class SarifFix
{
    public required SarifMessage Description { get; set; }
}

internal class SarifInvocation
{
    public bool ExecutionSuccessful { get; set; }
    public string? StartTimeUtc { get; set; }
    public string? EndTimeUtc { get; set; }
    public SarifArtifactLocation? WorkingDirectory { get; set; }
}

#endregion
