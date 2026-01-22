using System.CommandLine;
using System.CommandLine.Invocation;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Core.Reporting;
using RuleKeeper.Core.Rules;
using RuleKeeper.Sdk;
using Spectre.Console;
using Language = RuleKeeper.Sdk.Language;

namespace RuleKeeper.Cli.Commands;

public static class ScanCommand
{
    public static Command Create()
    {
        var pathArgument = new Argument<string>(
            name: "path",
            description: "Path to scan (file, directory, project, or solution)",
            getDefaultValue: () => ".");

        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file");

        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output format (console, json, sarif, html)",
            getDefaultValue: () => "console");

        var additionalFormatsOption = new Option<string[]>(
            aliases: ["--format", "-F"],
            description: "Additional output formats to generate (json, sarif, html)")
        { AllowMultipleArgumentsPerToken = true };

        var severityOption = new Option<SeverityLevel?>(
            aliases: ["--severity", "-s"],
            description: "Minimum severity to report");

        var failOnOption = new Option<SeverityLevel?>(
            aliases: ["--fail-on", "-f"],
            description: "Severity level that causes non-zero exit code");

        // Threshold options
        var thresholdPercentOption = new Option<double>(
            aliases: ["--threshold-percent", "--tp"],
            description: "Maximum allowed violation percentage (0-100). Default 0 means any violation fails",
            getDefaultValue: () => 0);

        var criticalThresholdOption = new Option<int>(
            aliases: new[] { "--critical-threshold", "--ct" },
            description: "Maximum allowed critical violations. Default 0 means any critical fails",
            getDefaultValue: () => 0);

        var highThresholdOption = new Option<int>(
            aliases: new[] { "--high-threshold", "--ht" },
            description: "Maximum allowed high severity violations. Default 0 means any high fails",
            getDefaultValue: () => 0);

        var totalThresholdOption = new Option<int>(
            aliases: new[] { "--total-threshold", "--tt" },
            description: "Maximum allowed total violations. Default 0 means any violation fails",
            getDefaultValue: () => 0);

        var outputFileOption = new Option<string?>(
            aliases: new[] { "--output-file" },
            description: "Output file path (required for non-console formats)");

        var includeOption = new Option<string[]>(
            aliases: new[] { "--include", "-i" },
            description: "File patterns to include")
        { AllowMultipleArgumentsPerToken = true };

        var excludeOption = new Option<string[]>(
            aliases: new[] { "--exclude", "-e" },
            description: "File patterns to exclude")
        { AllowMultipleArgumentsPerToken = true };

        var parallelOption = new Option<bool>(
            aliases: new[] { "--parallel", "-p" },
            description: "Enable parallel analysis",
            getDefaultValue: () => true);

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Verbose output");

        var noCacheOption = new Option<bool>(
            aliases: new[] { "--no-cache" },
            description: "Disable caching");

        var noColorOption = new Option<bool>(
            aliases: new[] { "--no-color" },
            description: "Disable colored output");

        // Visualization options
        var noVisualizationOption = new Option<bool>(
            aliases: new[] { "--no-viz", "--no-visualization" },
            description: "Disable visualization in reports");

        var noTableOption = new Option<bool>(
            aliases: new[] { "--no-table" },
            description: "Disable tabular summary in console output");

        var summaryOnlyOption = new Option<bool>(
            aliases: new[] { "--summary-only", "--summary" },
            description: "Show only summary tables without individual violation details");

        // Language options
        var languageOption = new Option<Language>(
            aliases: new[] { "--language", "-l" },
            description: "Primary programming language to analyze (csharp, python, javascript, typescript, java, go)",
            getDefaultValue: () => Language.CSharp);

        var languagesOption = new Option<string[]>(
            aliases: new[] { "--languages", "-L" },
            description: "Multiple programming languages to analyze (comma-separated or repeated)")
        { AllowMultipleArgumentsPerToken = true };

        var command = new Command("scan", "Scan code for policy violations")
        {
            pathArgument,
            configOption,
            outputOption,
            additionalFormatsOption,
            severityOption,
            failOnOption,
            thresholdPercentOption,
            criticalThresholdOption,
            highThresholdOption,
            totalThresholdOption,
            outputFileOption,
            includeOption,
            excludeOption,
            parallelOption,
            verboseOption,
            noCacheOption,
            noColorOption,
            noVisualizationOption,
            noTableOption,
            summaryOnlyOption,
            languageOption,
            languagesOption
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var outputFormat = context.ParseResult.GetValueForOption(outputOption);
            var additionalFormats = context.ParseResult.GetValueForOption(additionalFormatsOption) ?? Array.Empty<string>();
            var minSeverity = context.ParseResult.GetValueForOption(severityOption);
            var failOn = context.ParseResult.GetValueForOption(failOnOption);
            var thresholdPercent = context.ParseResult.GetValueForOption(thresholdPercentOption);
            var criticalThreshold = context.ParseResult.GetValueForOption(criticalThresholdOption);
            var highThreshold = context.ParseResult.GetValueForOption(highThresholdOption);
            var totalThreshold = context.ParseResult.GetValueForOption(totalThresholdOption);
            var outputFile = context.ParseResult.GetValueForOption(outputFileOption);
            var includes = context.ParseResult.GetValueForOption(includeOption) ?? Array.Empty<string>();
            var excludes = context.ParseResult.GetValueForOption(excludeOption) ?? Array.Empty<string>();
            var parallel = context.ParseResult.GetValueForOption(parallelOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var noCache = context.ParseResult.GetValueForOption(noCacheOption);
            var noColor = context.ParseResult.GetValueForOption(noColorOption);
            var noVisualization = context.ParseResult.GetValueForOption(noVisualizationOption);
            var noTable = context.ParseResult.GetValueForOption(noTableOption);
            var summaryOnly = context.ParseResult.GetValueForOption(summaryOnlyOption);
            var language = context.ParseResult.GetValueForOption(languageOption);
            var languagesStr = context.ParseResult.GetValueForOption(languagesOption) ?? Array.Empty<string>();

            // Parse languages from string array
            var languages = ParseLanguages(languagesStr, language);

            var exitCode = await ExecuteAsync(
                path, configPath, outputFormat, additionalFormats, minSeverity, failOn,
                thresholdPercent, criticalThreshold, highThreshold, totalThreshold,
                outputFile, includes, excludes, parallel, verbose, noCache, noColor,
                noVisualization, noTable, summaryOnly, languages,
                context.GetCancellationToken());

            context.ExitCode = exitCode;
        });

        return command;
    }

    private static List<Language> ParseLanguages(string[] languagesStr, Language defaultLanguage)
    {
        if (languagesStr.Length == 0)
        {
            return [defaultLanguage];
        }

        var languages = new List<Language>();
        foreach (var langStr in languagesStr)
        {
            var parts = langStr.Split(',', StringSplitOptions.RemoveEmptyEntries); // Comma Seperated Values
            foreach (var part in parts)
            {
                if (Enum.TryParse<Language>(part.Trim(), ignoreCase: true, out var lang))
                {
                    if (!languages.Contains(lang))
                        languages.Add(lang);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Unknown language '{part.Trim()}', skipping.");
                }
            }
        }

        return languages.Count > 0 ? languages : [defaultLanguage];
    }

    private static async Task<int> ExecuteAsync(
        string path,
        string? configPath,
        string outputFormat,
        string[] additionalFormats,
        SeverityLevel? minSeverity,
        SeverityLevel? failOn,
        double thresholdPercent,
        int criticalThreshold,
        int highThreshold,
        int totalThreshold,
        string? outputFile,
        string[] includes,
        string[] excludes,
        bool parallel,
        bool verbose,
        bool noCache,
        bool noColor,
        bool noVisualization,
        bool noTable,
        bool summaryOnly,
        List<Language> languages,
        CancellationToken cancellationToken)
    {
        try
        {
            // Resolve path
            path = Path.GetFullPath(path);
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Path not found: {path}");
                return 1;
            }

            // Load configuration
            var configLoader = new ConfigurationLoader();
            RuleKeeperConfig config;
            string? usedConfigPath;

            if (!string.IsNullOrEmpty(configPath))
            {
                config = configLoader.LoadFromFile(configPath);
                usedConfigPath = configPath;
            }
            else
            {
                var startDir = Directory.Exists(path) ? path : Path.GetDirectoryName(path) ?? ".";
                (config, usedConfigPath) = configLoader.LoadFromDirectory(startDir);
            }

            // Apply command-line overrides
            ApplyOverrides(config, minSeverity, failOn, thresholdPercent, criticalThreshold,
                highThreshold, totalThreshold, includes, excludes, parallel, noCache, noColor,
                noVisualization, noTable, summaryOnly, languages, additionalFormats);

            if (verbose)
            {
                AnsiConsole.MarkupLine($"[blue]Path:[/] {path}");
                AnsiConsole.MarkupLine($"[blue]Config:[/] {usedConfigPath ?? "default"}");
                AnsiConsole.MarkupLine($"[blue]Output:[/] {outputFormat}");
                AnsiConsole.MarkupLine($"[blue]Languages:[/] {string.Join(", ", languages)}");
                if (additionalFormats.Length > 0)
                    AnsiConsole.MarkupLine($"[blue]Additional formats:[/] {string.Join(", ", additionalFormats)}");
                if (thresholdPercent > 0 || criticalThreshold > 0 || highThreshold > 0 || totalThreshold > 0)
                {
                    AnsiConsole.MarkupLine($"[blue]Thresholds:[/] Critical={criticalThreshold}, High={highThreshold}, Total={totalThreshold}, Percent={thresholdPercent}%");
                }
                AnsiConsole.WriteLine();
            }

            // Initialize rule registry
            var registry = new RuleRegistry();
            registry.RegisterAssembly(typeof(RuleKeeper.Rules.Naming.ClassNamingAnalyzer).Assembly);
            registry.LoadCustomRules(config);

            // Run analysis with progress
            AnalysisReport report;

            using var engine = new AnalysisEngine(registry);

            if (!noColor && outputFormat == "console")
            {
                report = await AnsiConsole.Progress()
                    .AutoClear(true)
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn())
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("Analyzing...");

                        var progress = new Progress<AnalysisProgress>(p =>
                        {
                            task.Description = $"Analyzing: {Path.GetFileName(p.CurrentFile ?? "")}";
                            task.Value = p.Percentage;
                        });

                        return await engine.AnalyzeAsync(path, config, progress, cancellationToken);
                    });
            }
            else
            {
                report = await engine.AnalyzeAsync(path, config, null, cancellationToken);
            }

            report = report with { ConfigFilePath = usedConfigPath };

            // Generate and output primary report
            var reporter = ReportGeneratorFactory.Create(outputFormat);
            var reportContent = reporter.Generate(report, config.Output);

            if (!string.IsNullOrEmpty(outputFile))
            {
                await reporter.WriteToFileAsync(report, config.Output, outputFile);
                if (outputFormat == "console")
                {
                    AnsiConsole.MarkupLine($"[green]Report written to:[/] {outputFile}");
                }
            }
            else if (outputFormat != "console")
            {
                // For non-console formats without output file, write to stdout
                Console.WriteLine(reportContent);
            }
            else
            {
                // Console output
                Console.Write(reportContent);
            }

            // Generate additional format reports
            foreach (var format in additionalFormats)
            {
                var additionalReporter = ReportGeneratorFactory.Create(format);
                var outputPath = outputFile != null
                    ? Path.ChangeExtension(outputFile, GetExtension(format))
                    : $"rulekeeper-report.{GetExtension(format)}";

                await additionalReporter.WriteToFileAsync(report, config.Output, outputPath);
                AnsiConsole.MarkupLine($"[green]{format.ToUpper()} report written to:[/] {outputPath}");
            }

            // Determine exit code using thresholds
            var summary = report.GetSummary();
            var exceedsThreshold = summary.ExceedsThresholds(
                config.Output.FailOn,
                config.Output.CriticalThreshold,
                config.Output.HighThreshold,
                config.Output.TotalThreshold,
                config.Output.ThresholdPercentage);

            if (exceedsThreshold)
            {
                var status = summary.GetStatus(
                    config.Output.FailOn,
                    config.Output.CriticalThreshold,
                    config.Output.HighThreshold,
                    config.Output.TotalThreshold,
                    config.Output.ThresholdPercentage);
                AnsiConsole.MarkupLine($"[red]{status}[/]");
                return 1;
            }

            return 0;
        }
        catch (ConfigurationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Configuration Error:[/] {ex.Message}");
            return 2;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Analysis cancelled.[/]");
            return 130;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (verbose)
            {
                AnsiConsole.WriteException(ex);
            }
            return 1;
        }
    }

    private static string GetExtension(string format)
    {
        return format.ToLower() switch
        {
            "json" => "json",
            "sarif" => "sarif.json",
            "html" => "html",
            _ => "txt"
        };
    }

    private static void ApplyOverrides(
        RuleKeeperConfig config,
        SeverityLevel? minSeverity,
        SeverityLevel? failOn,
        double thresholdPercent,
        int criticalThreshold,
        int highThreshold,
        int totalThreshold,
        string[] includes,
        string[] excludes,
        bool parallel,
        bool noCache,
        bool noColor,
        bool noVisualization,
        bool noTable,
        bool summaryOnly,
        List<Language> languages,
        string[] additionalFormats)
    {
        if (minSeverity.HasValue)
            config.Output.MinSeverity = minSeverity.Value;

        if (failOn.HasValue)
            config.Output.FailOn = failOn.Value;

        // Apply threshold overrides
        if (thresholdPercent > 0)
            config.Output.ThresholdPercentage = thresholdPercent;
        if (criticalThreshold > 0)
            config.Output.CriticalThreshold = criticalThreshold;
        if (highThreshold > 0)
            config.Output.HighThreshold = highThreshold;
        if (totalThreshold > 0)
            config.Output.TotalThreshold = totalThreshold;

        if (includes.Length > 0)
            config.Scan.Include = includes.ToList();

        if (excludes.Length > 0)
            config.Scan.Exclude.AddRange(excludes);

        config.Scan.Parallel = parallel;
        config.Scan.Cache = !noCache;
        config.Output.Colors = !noColor;
        config.Output.Visualization = !noVisualization;
        config.Output.ShowTable = !noTable;
        config.Output.SummaryOnly = summaryOnly;

        // Apply languages
        if (languages.Count > 0)
        {
            config.Scan.Languages = languages;
            // Also set the primary language for backward compatibility
            config.Scan.Language = languages[0];
        }

        if (additionalFormats.Length > 0)
            config.Output.AdditionalFormats = additionalFormats.ToList();
    }
}
