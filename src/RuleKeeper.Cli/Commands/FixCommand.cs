using System.CommandLine;
using System.CommandLine.Invocation;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Core.Fixing;
using RuleKeeper.Core.Rules;
using RuleKeeper.Sdk;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

/// <summary>
/// Command to automatically fix code violations detected by RuleKeeper.
/// </summary>
public static class FixCommand
{
    public static Command Create()
    {
        var pathArgument = new Argument<string>(
            name: "path",
            description: "Path to the file, directory, or project to fix",
            getDefaultValue: () => ".");

        var configOption = new Option<string?>(
            aliases: new[] { "--config", "-c" },
            description: "Path to RuleKeeper configuration file");

        var ruleOption = new Option<string?>(
            aliases: new[] { "--rule", "-r" },
            description: "Fix only violations of a specific rule ID (e.g., CS-NAME-001)");

        var categoryOption = new Option<string?>(
            aliases: new[] { "--category", "-t" },
            description: "Fix only violations in a specific category (e.g., naming, async, security)");

        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-d" },
            description: "Preview fixes without applying them");

        var noBackupOption = new Option<bool>(
            aliases: new[] { "--no-backup" },
            description: "Don't create backup files before fixing");

        var interactiveOption = new Option<bool>(
            aliases: new[] { "--interactive", "-i" },
            description: "Confirm each fix before applying");

        var listFixableOption = new Option<bool>(
            aliases: new[] { "--list-fixable", "-l" },
            description: "List all fixable rule IDs and exit");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed output");

        var command = new Command("fix", "Automatically fix code violations")
        {
            pathArgument,
            configOption,
            ruleOption,
            categoryOption,
            dryRunOption,
            noBackupOption,
            interactiveOption,
            listFixableOption,
            verboseOption
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var rule = context.ParseResult.GetValueForOption(ruleOption);
            var category = context.ParseResult.GetValueForOption(categoryOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var noBackup = context.ParseResult.GetValueForOption(noBackupOption);
            var interactive = context.ParseResult.GetValueForOption(interactiveOption);
            var listFixable = context.ParseResult.GetValueForOption(listFixableOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var exitCode = await ExecuteAsync(
                path, configPath, rule, category, dryRun, noBackup,
                interactive, listFixable, verbose, context.GetCancellationToken());

            context.ExitCode = exitCode;
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string path,
        string? configPath,
        string? ruleFilter,
        string? categoryFilter,
        bool dryRun,
        bool noBackup,
        bool interactive,
        bool listFixable,
        bool verbose,
        CancellationToken cancellationToken)
    {
        try
        {
            // Initialize fix providers
            FixProviderRegistry.Initialize();

            // Load configuration
            var loader = new ConfigurationLoader();
            RuleKeeperConfig config;

            if (!string.IsNullOrEmpty(configPath))
            {
                config = loader.LoadFromFile(configPath);
            }
            else
            {
                var (loadedConfig, foundPath) = loader.LoadFromDirectory(Directory.GetCurrentDirectory());
                config = loadedConfig;
                if (foundPath != null && verbose)
                {
                    AnsiConsole.MarkupLine($"[blue]Using config:[/] {foundPath}");
                }
            }

            // Load custom fix providers from configuration
            LoadCustomFixProviders(config, verbose);

            // List fixable rules if requested (after loading custom providers)
            if (listFixable)
            {
                ListFixableRules(verbose);
                return 0;
            }

            // First, run analysis to find violations
            AnsiConsole.MarkupLine("[blue]Scanning for violations...[/]");

            // Initialize rule registry (same as ScanCommand)
            var registry = new RuleRegistry();
            registry.RegisterAssembly(typeof(RuleKeeper.Rules.Naming.ClassNamingAnalyzer).Assembly);
            registry.LoadCustomRules(config);

            var engine = new AnalysisEngine(registry);
            var report = await engine.AnalyzeAsync(path, config, null, cancellationToken);

            if (verbose)
            {
                AnsiConsole.MarkupLine($"[dim]Path: {Path.GetFullPath(path)}[/]");
                AnsiConsole.MarkupLine($"[dim]Files analyzed: {report.AnalyzedFiles.Count}[/]");
            }

            if (report.Violations.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]No violations found![/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[yellow]Found {report.Violations.Count} violation(s)[/]");

            // Create fixer
            var fixer = FixProviderRegistry.CreateFixer(createBackups: !noBackup, dryRun: dryRun);

            // Get available fixes
            var violations = report.Violations.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(ruleFilter))
            {
                violations = violations.Where(v =>
                    v.RuleId.Equals(ruleFilter, StringComparison.OrdinalIgnoreCase));
                AnsiConsole.MarkupLine($"[blue]Filtering by rule:[/] {ruleFilter}");
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                violations = violations.Where(v =>
                    v.RuleId.Contains($"-{categoryFilter.ToUpper()}-", StringComparison.OrdinalIgnoreCase));
                AnsiConsole.MarkupLine($"[blue]Filtering by category:[/] {categoryFilter}");
            }

            var filteredViolations = violations.ToList();
            var availableFixes = fixer.GetAvailableFixes(filteredViolations, ruleFilter, categoryFilter);

            if (availableFixes.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No automatic fixes available for the detected violations.[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("Run [blue]rulekeeper fix --list-fixable[/] to see which rules support auto-fixing.");
                return 0;
            }

            // Display available fixes
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Found {availableFixes.Count} fix(es) available[/]");

            if (verbose || dryRun)
            {
                DisplayFixesTable(availableFixes);
            }

            // Interactive mode
            if (interactive && !dryRun)
            {
                var selectedFixes = await SelectFixesInteractively(availableFixes, cancellationToken);
                if (selectedFixes.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No fixes selected.[/]");
                    return 0;
                }

                // Create a new list of violations that match selected fixes
                var selectedViolations = filteredViolations
                    .Where(v => selectedFixes.Any(f =>
                        f.FilePath == v.FilePath &&
                        f.StartLine == v.StartLine &&
                        f.RuleId == v.RuleId))
                    .ToList();

                filteredViolations = selectedViolations;
            }

            if (dryRun)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]DRY RUN - No changes were made[/]");
                return 0;
            }

            // Apply fixes
            AnsiConsole.WriteLine();
            var summary = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Applying fixes...", async ctx =>
                {
                    return await fixer.ApplyFixesAsync(filteredViolations, ruleFilter, categoryFilter, cancellationToken);
                });

            // Display results
            DisplayFixSummary(summary, verbose);

            return summary.FailedFixes > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static void LoadCustomFixProviders(RuleKeeperConfig config, bool verbose)
    {
        var customProviderPaths = new List<string>();

        // Load from custom_fix_providers if specified
        foreach (var source in config.CustomFixProviders)
        {
            if (!string.IsNullOrEmpty(source.Path))
            {
                customProviderPaths.Add(source.Path);
            }
        }

        // Also load from custom_rules assemblies (they may contain fix providers too)
        foreach (var source in config.CustomRules)
        {
            if (!string.IsNullOrEmpty(source.Path))
            {
                customProviderPaths.Add(source.Path);
            }
        }

        // Load all providers
        var totalLoaded = 0;
        foreach (var path in customProviderPaths.Distinct())
        {
            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(Directory.GetCurrentDirectory(), path);

            if (File.Exists(fullPath))
            {
                var count = FixProviderRegistry.LoadFromFile(fullPath);
                if (count > 0 && verbose)
                {
                    AnsiConsole.MarkupLine($"[blue]Loaded {count} fix provider(s) from:[/] {path}");
                }
                totalLoaded += count;
            }
            else if (verbose)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Fix provider assembly not found: {path}");
            }
        }

        if (totalLoaded > 0 && verbose)
        {
            AnsiConsole.MarkupLine($"[green]Total custom fix providers loaded: {totalLoaded}[/]");
        }
    }

    private static void ListFixableRules(bool verbose = false)
    {
        var ruleIds = FixProviderRegistry.GetAllSupportedRuleIds().ToList();
        var providerInfo = FixProviderRegistry.GetProviderInfo().ToList();

        AnsiConsole.MarkupLine("[bold]Fixable Rules[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Rule ID")
            .AddColumn("Category")
            .AddColumn("Description");

        if (verbose)
        {
            table.AddColumn("Provider");
        }

        var ruleDescriptions = new Dictionary<string, (string Category, string Description)>
        {
            ["CS-NAME-001"] = ("Naming", "Class naming (PascalCase)"),
            ["CS-NAME-002"] = ("Naming", "Method naming (PascalCase)"),
            ["CS-NAME-003"] = ("Naming", "Async method suffix (Async)"),
            ["CS-NAME-004"] = ("Naming", "Private field naming (_camelCase)"),
            ["CS-NAME-005"] = ("Naming", "Constant naming (PascalCase)"),
            ["CS-NAME-006"] = ("Naming", "Property naming (PascalCase)"),
            ["CS-NAME-007"] = ("Naming", "Interface prefix (I)"),
            ["CS-NAME-008"] = ("Naming", "Parameter naming (camelCase)"),
            ["CS-NAME-009"] = ("Naming", "Local variable naming (camelCase)"),
            ["CS-NAME-010"] = ("Naming", "Type parameter prefix (T)"),
            ["CS-ASYNC-002"] = ("Async", "Blocking calls (.Result/.Wait() → await)"),
            ["CS-ASYNC-003"] = ("Async", "Add ConfigureAwait(false)"),
            ["CS-EXC-001"] = ("Exceptions", "Empty catch blocks (add TODO comment)"),
            ["CS-EXC-004"] = ("Exceptions", "throw ex → throw (preserve stack trace)"),
        };

        foreach (var ruleId in ruleIds)
        {
            string category, description;
            if (ruleDescriptions.TryGetValue(ruleId, out var info))
            {
                category = info.Category;
                description = info.Description;
            }
            else
            {
                category = ruleId.Split('-').ElementAtOrDefault(1) ?? "Custom";
                description = "Auto-fixable";
            }

            if (verbose)
            {
                var provider = providerInfo.FirstOrDefault(p => p.SupportedRuleIds.Contains(ruleId));
                var providerName = provider?.IsBuiltIn == false
                    ? $"[yellow]{provider.Name}[/]"
                    : provider?.Name ?? "Unknown";

                table.AddRow(
                    $"[cyan]{ruleId}[/]",
                    category,
                    description,
                    providerName
                );
            }
            else
            {
                table.AddRow(
                    $"[cyan]{ruleId}[/]",
                    category,
                    description
                );
            }
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total: {ruleIds.Count} rules support auto-fixing[/]");

        // Show provider summary if verbose
        if (verbose && providerInfo.Any(p => !p.IsBuiltIn))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Registered Fix Providers:[/]");

            var providerTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Provider")
                .AddColumn("Category")
                .AddColumn("Rules")
                .AddColumn("Type");

            foreach (var provider in providerInfo)
            {
                providerTable.AddRow(
                    provider.Name,
                    provider.Category,
                    provider.SupportedRuleIds.Count.ToString(),
                    provider.IsBuiltIn ? "[dim]Built-in[/]" : "[yellow]Custom[/]"
                );
            }

            AnsiConsole.Write(providerTable);
        }
    }

    private static void DisplayFixesTable(List<CodeFix> fixes)
    {
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("File")
            .AddColumn("Line")
            .AddColumn("Rule")
            .AddColumn("Fix");

        foreach (var fix in fixes.Take(50)) // Limit display
        {
            var shortPath = Path.GetFileName(fix.FilePath);
            table.AddRow(
                $"[dim]{shortPath}[/]",
                fix.StartLine.ToString(),
                $"[cyan]{fix.RuleId}[/]",
                Markup.Escape(fix.Description.Length > 50
                    ? fix.Description.Substring(0, 47) + "..."
                    : fix.Description)
            );
        }

        AnsiConsole.Write(table);

        if (fixes.Count > 50)
        {
            AnsiConsole.MarkupLine($"[dim]... and {fixes.Count - 50} more fixes[/]");
        }
    }

    private static Task<List<CodeFix>> SelectFixesInteractively(
        List<CodeFix> fixes,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
        var selected = new List<CodeFix>();

        // Group by rule for easier selection
        var byRule = fixes.GroupBy(f => f.RuleId).ToList();

        // First, ask about applying by rule
        var ruleChoices = byRule.Select(g => $"{g.Key} ({g.Count()} fixes)").ToList();
        ruleChoices.Insert(0, "All fixes");
        ruleChoices.Add("Select individually");
        ruleChoices.Add("Cancel");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to apply fixes?")
                .AddChoices(ruleChoices));

        if (choice == "Cancel")
        {
            return selected;
        }

        if (choice == "All fixes")
        {
            return fixes;
        }

        if (choice == "Select individually")
        {
            foreach (var fix in fixes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var shortPath = Path.GetFileName(fix.FilePath);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[cyan]{fix.RuleId}[/] in [dim]{shortPath}:{fix.StartLine}[/]");
                AnsiConsole.MarkupLine($"  {Markup.Escape(fix.Description)}");

                if (AnsiConsole.Confirm("Apply this fix?", defaultValue: true))
                {
                    selected.Add(fix);
                }
            }
        }
        else
        {
            // Apply all fixes for selected rule
            var selectedRule = choice.Split(' ')[0];
            selected.AddRange(fixes.Where(f => f.RuleId == selectedRule));
        }

            return selected;
        });
    }

    private static void DisplayFixSummary(FixSummary summary, bool verbose)
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(new Markup(
            $"[green]Fixes applied:[/] {summary.SuccessfulFixes}\n" +
            $"[red]Fixes failed:[/] {summary.FailedFixes}\n" +
            $"[blue]Files modified:[/] {summary.FilesModified}\n" +
            $"[dim]Duration:[/] {summary.Duration.TotalMilliseconds:F0}ms"))
        {
            Header = new PanelHeader("[bold] Fix Summary [/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);

        if (summary.FixesByRule.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Fixes by Rule:[/]");

            var table = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Rule")
                .AddColumn("Fixed");

            foreach (var (rule, count) in summary.FixesByRule.OrderByDescending(kv => kv.Value))
            {
                table.AddRow($"[cyan]{rule}[/]", count.ToString());
            }

            AnsiConsole.Write(table);
        }

        if (verbose && summary.Results.Any(r => !r.Success))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold red]Failed Fixes:[/]");

            foreach (var failed in summary.Results.Where(r => !r.Success))
            {
                var shortPath = Path.GetFileName(failed.FilePath);
                AnsiConsole.MarkupLine($"  [red]✗[/] {failed.Fix.RuleId} in {shortPath}:{failed.Fix.StartLine}");
                AnsiConsole.MarkupLine($"    [dim]{failed.ErrorMessage}[/]");
            }
        }

        if (summary.SuccessfulFixes > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Fixes applied successfully![/]");
            AnsiConsole.MarkupLine("[dim]Run 'rulekeeper scan' to verify the fixes.[/]");
        }
    }
}
