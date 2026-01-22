using System.Text;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using Spectre.Console;

namespace RuleKeeper.Core.Reporting;

/// <summary>
/// Generates console-formatted reports with optional color support and tabular layout.
/// </summary>
public class ConsoleReporter : IReportGenerator
{
    public string Format => "console";

    public string Generate(AnalysisReport report, OutputConfig config)
    {
        var sb = new StringBuilder();

        // If colors are enabled and we're outputting to console, use Spectre.Console
        if (config.Colors)
        {
            return GenerateSpectreOutput(report, config);
        }

        // Plain text output
        return GeneratePlainTextOutput(report, config);
    }

    private string GenerateSpectreOutput(AnalysisReport report, OutputConfig config)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer)
        });

        // Header
        console.WriteLine();
        var headerRule = new Rule("[bold cyan]RuleKeeper Analysis Report[/]")
        {
            Justification = Justify.Center,
            Style = Style.Parse("cyan")
        };
        console.Write(headerRule);
        console.WriteLine();

        var summary = report.GetSummary();

        // Summary Table
        if (config.ShowTable)
        {
            var summaryTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .Title("[bold white]Summary[/]")
                .AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

            summaryTable.AddRow("Path", $"[blue]{Markup.Escape(report.AnalyzedPath)}[/]");
            summaryTable.AddRow("Config", $"[grey]{Markup.Escape(report.ConfigFilePath ?? "default")}[/]");
            summaryTable.AddRow("Files Analyzed", $"[white]{summary.TotalFiles}[/]");
            summaryTable.AddRow("Duration", $"[white]{summary.Duration.TotalSeconds:F2}s[/]");
            summaryTable.AddRow("Total Violations", GetColoredCount(summary.TotalViolations));

            console.Write(summaryTable);
            console.WriteLine();
        }

        // Severity Breakdown Table
        if (config.ShowTable)
        {
            var severityTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .Title("[bold white]Violations by Severity[/]")
                .AddColumn(new TableColumn("[bold]Severity[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Count[/]").Centered())
                .AddColumn(new TableColumn("[bold]Status[/]").RightAligned());

            severityTable.AddRow(
                "[bold red on white] CRITICAL [/]",
                summary.CriticalCount.ToString(),
                GetStatusIndicator(summary.CriticalCount));
            severityTable.AddRow(
                "[red] HIGH [/]",
                summary.HighCount.ToString(),
                GetStatusIndicator(summary.HighCount));
            severityTable.AddRow(
                "[yellow] MEDIUM [/]",
                summary.MediumCount.ToString(),
                GetStatusIndicator(summary.MediumCount));
            severityTable.AddRow(
                "[cyan] LOW [/]",
                summary.LowCount.ToString(),
                GetStatusIndicator(summary.LowCount));
            severityTable.AddRow(
                "[grey] INFO [/]",
                summary.InfoCount.ToString(),
                GetStatusIndicator(summary.InfoCount));

            console.Write(severityTable);
            console.WriteLine();
        }

        // Visualization - Bar Chart
        if (config.Visualization && summary.TotalViolations > 0)
        {
            console.Write(new Rule("[bold white]Severity Distribution[/]") { Style = Style.Parse("grey") });

            var chart = new BarChart()
                .Width(60)
                .Label("[bold white]Violations[/]")
                .CenterLabel();

            if (summary.CriticalCount > 0)
                chart.AddItem("Critical", summary.CriticalCount, Color.Red);
            if (summary.HighCount > 0)
                chart.AddItem("High", summary.HighCount, Color.Orange1);
            if (summary.MediumCount > 0)
                chart.AddItem("Medium", summary.MediumCount, Color.Yellow);
            if (summary.LowCount > 0)
                chart.AddItem("Low", summary.LowCount, Color.Cyan1);
            if (summary.InfoCount > 0)
                chart.AddItem("Info", summary.InfoCount, Color.Grey);

            console.Write(chart);
            console.WriteLine();
        }

        // Violations by Rule Table
        if (report.Violations.Count > 0 && config.ShowTable)
        {
            var ruleGroups = report.ViolationsByRule.OrderByDescending(g => g.Value.Count).Take(10);

            var ruleTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .Title("[bold white]Top Violations by Rule[/]")
                .AddColumn(new TableColumn("[bold]Rule ID[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Rule Name[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Count[/]").Centered())
                .AddColumn(new TableColumn("[bold]Severity[/]").RightAligned());

            foreach (var group in ruleGroups)
            {
                var firstViolation = group.Value.First();
                ruleTable.AddRow(
                    $"[cyan]{Markup.Escape(group.Key)}[/]",
                    Markup.Escape(firstViolation.RuleName ?? group.Key).Length > 40
                        ? Markup.Escape(firstViolation.RuleName ?? group.Key)[..37] + "..."
                        : Markup.Escape(firstViolation.RuleName ?? group.Key),
                    group.Value.Count.ToString(),
                    GetSeverityMarkup(firstViolation.Severity));
            }

            console.Write(ruleTable);
            console.WriteLine();
        }

        // Detailed Violations (skip if summary only)
        if (report.Violations.Count > 0 && !config.SummaryOnly)
        {
            console.Write(new Rule("[bold white]Violations Detail[/]") { Style = Style.Parse("grey") });
            console.WriteLine();

            var maxViolations = config.MaxTotal ?? int.MaxValue;
            var displayedCount = 0;

            foreach (var fileGroup in report.ViolationsByFile.OrderBy(g => g.Key))
            {
                if (displayedCount >= maxViolations) break;

                var relativePath = GetRelativePath(fileGroup.Key, report.AnalyzedPath);
                console.MarkupLine($"[bold blue]{Markup.Escape(relativePath)}[/]");

                var fileViolations = fileGroup.Value.OrderBy(v => v.StartLine);
                var perRuleCount = new Dictionary<string, int>();

                foreach (var violation in fileViolations)
                {
                    if (displayedCount >= maxViolations) break;

                    // Check per-rule limit
                    if (config.MaxPerRule.HasValue)
                    {
                        perRuleCount.TryGetValue(violation.RuleId, out var count);
                        if (count >= config.MaxPerRule.Value) continue;
                        perRuleCount[violation.RuleId] = count + 1;
                    }

                    var violationTable = new Table()
                        .Border(TableBorder.None)
                        .HideHeaders()
                        .AddColumn("")
                        .AddColumn("");

                    var locationStr = $"[grey]Line {violation.StartLine}:{violation.StartColumn}[/]";
                    var severityBadge = GetSeverityBadge(violation.Severity);
                    var ruleIdStr = $"[cyan][[{Markup.Escape(violation.RuleId)}]][/]";

                    console.MarkupLine($"  {locationStr} {severityBadge} {ruleIdStr} {Markup.Escape(violation.Message)}");

                    if (config.ShowCode && !string.IsNullOrEmpty(violation.CodeSnippet))
                    {
                        console.MarkupLine($"    [grey]{Markup.Escape(violation.CodeSnippet.Trim())}[/]");
                    }

                    if (config.ShowHints && !string.IsNullOrEmpty(violation.FixHint))
                    {
                        console.MarkupLine($"    [green]Hint: {Markup.Escape(violation.FixHint)}[/]");
                    }

                    displayedCount++;
                }

                console.WriteLine();
            }

            if (displayedCount < summary.TotalViolations)
            {
                console.MarkupLine($"[grey]... and {summary.TotalViolations - displayedCount} more violation(s)[/]");
                console.WriteLine();
            }
        }

        // Errors
        if (report.Errors.Count > 0)
        {
            console.Write(new Rule("[bold red]Errors[/]") { Style = Style.Parse("red") });

            var errorTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Red)
                .AddColumn("[bold]File[/]")
                .AddColumn("[bold]Error[/]");

            foreach (var error in report.Errors)
            {
                errorTable.AddRow(
                    $"[red]{Markup.Escape(error.FilePath)}[/]",
                    Markup.Escape(error.Message));
            }

            console.Write(errorTable);
            console.WriteLine();
        }

        // Footer with status
        var footerRule = new Rule() { Style = Style.Parse("grey") };
        console.Write(footerRule);

        var status = summary.GetStatus(config.FailOn, config.CriticalThreshold,
            config.HighThreshold, config.TotalThreshold, config.ThresholdPercentage);

        if (status.StartsWith("FAILED"))
        {
            console.MarkupLine($"[bold red]{Markup.Escape(status)}[/]");
        }
        else if (summary.TotalViolations == 0)
        {
            console.MarkupLine($"[bold green]{Markup.Escape(status)}[/]");
        }
        else
        {
            console.MarkupLine($"[bold yellow]{Markup.Escape(status)}[/]");
        }

        return writer.ToString();
    }

    private string GeneratePlainTextOutput(AnalysisReport report, OutputConfig config)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine();
        sb.AppendLine("════════════════════════════════════════════════════════════");
        sb.AppendLine("              RULEKEEPER ANALYSIS REPORT");
        sb.AppendLine("════════════════════════════════════════════════════════════");
        sb.AppendLine();

        var summary = report.GetSummary();

        // Summary
        if (config.ShowTable)
        {
            sb.AppendLine("SUMMARY");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            sb.AppendLine($"  Path:             {report.AnalyzedPath}");
            sb.AppendLine($"  Config:           {report.ConfigFilePath ?? "default"}");
            sb.AppendLine($"  Files Analyzed:   {summary.TotalFiles}");
            sb.AppendLine($"  Duration:         {summary.Duration.TotalSeconds:F2}s");
            sb.AppendLine($"  Total Violations: {summary.TotalViolations}");
            sb.AppendLine();
        }

        // Severity Breakdown
        if (config.ShowTable)
        {
            sb.AppendLine("VIOLATIONS BY SEVERITY");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            sb.AppendLine($"  ┌──────────────┬───────┬────────────┐");
            sb.AppendLine($"  │ Severity     │ Count │ Status     │");
            sb.AppendLine($"  ├──────────────┼───────┼────────────┤");
            sb.AppendLine($"  │ CRITICAL     │ {summary.CriticalCount,5} │ {(summary.CriticalCount > 0 ? "FAIL" : "PASS"),-10} │");
            sb.AppendLine($"  │ HIGH         │ {summary.HighCount,5} │ {(summary.HighCount > 0 ? "WARN" : "PASS"),-10} │");
            sb.AppendLine($"  │ MEDIUM       │ {summary.MediumCount,5} │ {(summary.MediumCount > 0 ? "WARN" : "PASS"),-10} │");
            sb.AppendLine($"  │ LOW          │ {summary.LowCount,5} │ {(summary.LowCount > 0 ? "INFO" : "PASS"),-10} │");
            sb.AppendLine($"  │ INFO         │ {summary.InfoCount,5} │ {(summary.InfoCount > 0 ? "INFO" : "PASS"),-10} │");
            sb.AppendLine($"  └──────────────┴───────┴────────────┘");
            sb.AppendLine();
        }

        // Visualization - ASCII Bar Chart
        if (config.Visualization && summary.TotalViolations > 0)
        {
            sb.AppendLine("SEVERITY DISTRIBUTION");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            var max = Math.Max(1, new[] { summary.CriticalCount, summary.HighCount, summary.MediumCount, summary.LowCount, summary.InfoCount }.Max());
            var scale = 40.0 / max;

            if (summary.CriticalCount > 0)
                sb.AppendLine($"  Critical │ {new string('█', (int)(summary.CriticalCount * scale))} {summary.CriticalCount}");
            if (summary.HighCount > 0)
                sb.AppendLine($"  High     │ {new string('█', (int)(summary.HighCount * scale))} {summary.HighCount}");
            if (summary.MediumCount > 0)
                sb.AppendLine($"  Medium   │ {new string('█', (int)(summary.MediumCount * scale))} {summary.MediumCount}");
            if (summary.LowCount > 0)
                sb.AppendLine($"  Low      │ {new string('█', (int)(summary.LowCount * scale))} {summary.LowCount}");
            if (summary.InfoCount > 0)
                sb.AppendLine($"  Info     │ {new string('█', (int)(summary.InfoCount * scale))} {summary.InfoCount}");
            sb.AppendLine();
        }

        // Violations (skip if summary only)
        if (report.Violations.Count > 0 && !config.SummaryOnly)
        {
            sb.AppendLine("VIOLATIONS DETAIL");
            sb.AppendLine("────────────────────────────────────────────────────────────");

            foreach (var fileGroup in report.ViolationsByFile.OrderBy(g => g.Key))
            {
                var relativePath = GetRelativePath(fileGroup.Key, report.AnalyzedPath);
                sb.AppendLine($"  {relativePath}");

                foreach (var violation in fileGroup.Value.OrderBy(v => v.StartLine))
                {
                    var severity = violation.Severity.ToString().ToUpper().Substring(0, Math.Min(4, violation.Severity.ToString().Length));
                    sb.AppendLine($"    {violation.StartLine}:{violation.StartColumn} [{severity}] [{violation.RuleId}] {violation.Message}");

                    if (config.ShowCode && !string.IsNullOrEmpty(violation.CodeSnippet))
                    {
                        sb.AppendLine($"      {violation.CodeSnippet.Trim()}");
                    }

                    if (config.ShowHints && !string.IsNullOrEmpty(violation.FixHint))
                    {
                        sb.AppendLine($"      Hint: {violation.FixHint}");
                    }
                }
                sb.AppendLine();
            }
        }

        // Errors
        if (report.Errors.Count > 0)
        {
            sb.AppendLine("ERRORS");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            foreach (var error in report.Errors)
            {
                sb.AppendLine($"  {error.FilePath}: {error.Message}");
            }
            sb.AppendLine();
        }

        // Footer
        sb.AppendLine("════════════════════════════════════════════════════════════");
        var status = summary.GetStatus(config.FailOn, config.CriticalThreshold,
            config.HighThreshold, config.TotalThreshold, config.ThresholdPercentage);
        sb.AppendLine(status);

        return sb.ToString();
    }

    public async Task WriteToFileAsync(AnalysisReport report, OutputConfig config, string filePath)
    {
        // For console format, disable colors when writing to file
        var originalColors = config.Colors;
        config.Colors = false;
        var content = Generate(report, config);
        config.Colors = originalColors;
        await File.WriteAllTextAsync(filePath, content);
    }

    private static string GetColoredCount(int count)
    {
        return count switch
        {
            0 => "[green]0[/]",
            < 5 => $"[yellow]{count}[/]",
            < 10 => $"[orange1]{count}[/]",
            _ => $"[red]{count}[/]"
        };
    }

    private static string GetStatusIndicator(int count)
    {
        return count switch
        {
            0 => "[green]PASS[/]",
            _ => "[red]FAIL[/]"
        };
    }

    private static string GetSeverityMarkup(SeverityLevel severity)
    {
        return severity switch
        {
            SeverityLevel.Critical => "[bold red on white]CRITICAL[/]",
            SeverityLevel.High => "[red]HIGH[/]",
            SeverityLevel.Medium => "[yellow]MEDIUM[/]",
            SeverityLevel.Low => "[cyan]LOW[/]",
            SeverityLevel.Info => "[grey]INFO[/]",
            _ => severity.ToString()
        };
    }

    private static string GetSeverityBadge(SeverityLevel severity)
    {
        return severity switch
        {
            SeverityLevel.Critical => "[bold white on red]CRIT[/]",
            SeverityLevel.High => "[red]HIGH[/]",
            SeverityLevel.Medium => "[yellow]MED [/]",
            SeverityLevel.Low => "[cyan]LOW [/]",
            SeverityLevel.Info => "[grey]INFO[/]",
            _ => "[grey]    [/]"
        };
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}
