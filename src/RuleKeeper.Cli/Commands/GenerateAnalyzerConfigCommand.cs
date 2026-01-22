using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

/// <summary>
/// Generates analyzer configuration files that developers can add to their projects
/// for IDE integration (Visual Studio, VS Code, Rider).
///
/// Supports:
/// - .globalconfig (modern approach, .NET 5+)
/// - .ruleset (legacy Roslyn ruleset)
/// - Directory.Build.props (MSBuild integration)
/// </summary>
public static class GenerateAnalyzerConfigCommand
{
    public static Command Create()
    {
        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            description: "Output format: globalconfig, ruleset, props, or all",
            getDefaultValue: () => "globalconfig");
        formatOption.AddCompletions("globalconfig", "ruleset", "props", "all");

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory (default: current directory)");

        var configOption = new Option<string?>(
            aliases: new[] { "--config", "-c" },
            description: "Path to RuleKeeper configuration file");

        var strictOption = new Option<bool>(
            aliases: new[] { "--strict" },
            description: "Use strict/error severity for all enabled rules");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force" },
            description: "Overwrite existing files without prompting");

        var command = new Command("generate-analyzer-config",
            "Generate IDE analyzer configuration files (.globalconfig, .ruleset, Directory.Build.props)")
        {
            formatOption,
            outputOption,
            configOption,
            strictOption,
            forceOption
        };

        command.AddAlias("gen-config");

        command.SetHandler((InvocationContext context) =>
        {
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption) ?? Directory.GetCurrentDirectory();
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var strict = context.ParseResult.GetValueForOption(strictOption);
            var force = context.ParseResult.GetValueForOption(forceOption);

            var exitCode = Execute(format, output, configPath, strict, force);
            context.ExitCode = exitCode;
        });

        return command;
    }

    private static int Execute(string format, string outputDir, string? configPath, bool strict, bool force)
    {
        try
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Load configuration
            RuleKeeperConfig? config = null;
            var loader = new ConfigurationLoader();

            if (!string.IsNullOrEmpty(configPath))
            {
                config = loader.LoadFromFile(configPath);
                AnsiConsole.MarkupLine($"[blue]Using config:[/] {configPath}");
            }
            else
            {
                var (foundConfig, foundPath) = loader.LoadFromDirectory(Directory.GetCurrentDirectory());
                if (foundPath != null)
                {
                    config = foundConfig;
                    AnsiConsole.MarkupLine($"[blue]Found config:[/] {foundPath}");
                }
            }

            var generatedFiles = new List<string>();

            if (format == "globalconfig" || format == "all")
            {
                var path = GenerateGlobalConfig(outputDir, config, strict, force);
                if (path != null) generatedFiles.Add(path);
            }

            if (format == "ruleset" || format == "all")
            {
                var path = GenerateRuleset(outputDir, config, strict, force);
                if (path != null) generatedFiles.Add(path);
            }

            if (format == "props" || format == "all")
            {
                var path = GenerateDirectoryBuildProps(outputDir, config, strict, force);
                if (path != null) generatedFiles.Add(path);
            }

            if (generatedFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No files were generated.[/]");
                return 1;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Generated files:[/]");
            foreach (var file in generatedFiles)
            {
                AnsiConsole.MarkupLine($"  • {file}");
            }

            AnsiConsole.WriteLine();
            PrintUsageInstructions(format);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static string? GenerateGlobalConfig(string outputDir, RuleKeeperConfig? config, bool strict, bool force)
    {
        var path = Path.Combine(outputDir, ".globalconfig");

        if (File.Exists(path) && !force)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping:[/] {path} already exists (use --force to overwrite)");
            return null;
        }

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Global Analyzer Configuration");
        sb.AppendLine("# Generated by RuleKeeper - https://github.com/rulekeeper");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("#");
        sb.AppendLine("# Add this file to your solution root to enforce rules across all projects.");
        sb.AppendLine("# For .NET 5+ projects, this file is automatically recognized.");
        sb.AppendLine();
        sb.AppendLine("is_global = true");
        sb.AppendLine();

        // Naming conventions
        sb.AppendLine("# ================================================");
        sb.AppendLine("# Naming Conventions (RuleKeeper: CS-NAME-*)");
        sb.AppendLine("# ================================================");
        sb.AppendLine();

        var namingSeverity = strict ? "error" : GetSeverityString(config, "naming", "warning");

        // IDE1006: Naming rule violation
        sb.AppendLine("# CS-NAME-*: Enforce naming conventions");
        sb.AppendLine($"dotnet_diagnostic.IDE1006.severity = {namingSeverity}");
        sb.AppendLine();

        // Async rules
        sb.AppendLine("# ================================================");
        sb.AppendLine("# Async Programming (RuleKeeper: CS-ASYNC-*)");
        sb.AppendLine("# ================================================");
        sb.AppendLine();

        var asyncSeverity = strict ? "error" : GetSeverityString(config, "async", "error");

        sb.AppendLine("# CS-ASYNC-001: Avoid async void");
        sb.AppendLine($"dotnet_diagnostic.VSTHRD101.severity = {asyncSeverity}");
        sb.AppendLine($"dotnet_diagnostic.ASYNC0001.severity = {asyncSeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-ASYNC-002: Avoid blocking calls (.Result, .Wait())");
        sb.AppendLine($"dotnet_diagnostic.VSTHRD002.severity = {asyncSeverity}");
        sb.AppendLine($"dotnet_diagnostic.VSTHRD103.severity = {asyncSeverity}");
        sb.AppendLine($"dotnet_diagnostic.ASYNC0002.severity = {asyncSeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-ASYNC-003: Configure await for library code");
        sb.AppendLine($"dotnet_diagnostic.CA2007.severity = suggestion");
        sb.AppendLine();

        // Security rules
        sb.AppendLine("# ================================================");
        sb.AppendLine("# Security (RuleKeeper: CS-SEC-*)");
        sb.AppendLine("# ================================================");
        sb.AppendLine();

        var securitySeverity = strict ? "error" : GetSeverityString(config, "security", "error");

        sb.AppendLine("# CS-SEC-001: SQL Injection");
        sb.AppendLine($"dotnet_diagnostic.CA2100.severity = {securitySeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-SEC-002: Hardcoded credentials");
        sb.AppendLine($"dotnet_diagnostic.CA2104.severity = {securitySeverity}");
        sb.AppendLine($"dotnet_diagnostic.SCS0015.severity = {securitySeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-SEC-003: Path traversal");
        sb.AppendLine($"dotnet_diagnostic.CA3003.severity = {securitySeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-SEC-004: XSS prevention");
        sb.AppendLine($"dotnet_diagnostic.CA3001.severity = {securitySeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-SEC-005: Insecure deserialization");
        sb.AppendLine($"dotnet_diagnostic.CA2300.severity = {securitySeverity}");
        sb.AppendLine($"dotnet_diagnostic.CA2301.severity = {securitySeverity}");
        sb.AppendLine($"dotnet_diagnostic.CA2302.severity = {securitySeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-SEC-006: Weak cryptography");
        sb.AppendLine($"dotnet_diagnostic.CA5350.severity = {securitySeverity}");
        sb.AppendLine($"dotnet_diagnostic.CA5351.severity = {securitySeverity}");
        sb.AppendLine();

        // Exception handling
        sb.AppendLine("# ================================================");
        sb.AppendLine("# Exception Handling (RuleKeeper: CS-EXC-*)");
        sb.AppendLine("# ================================================");
        sb.AppendLine();

        var exceptionSeverity = strict ? "error" : GetSeverityString(config, "exceptions", "warning");

        sb.AppendLine("# CS-EXC-002: Don't catch general exceptions");
        sb.AppendLine($"dotnet_diagnostic.CA1031.severity = {exceptionSeverity}");
        sb.AppendLine();

        sb.AppendLine("# CS-EXC-001: Don't swallow exceptions");
        sb.AppendLine($"dotnet_diagnostic.CA2200.severity = {exceptionSeverity}");
        sb.AppendLine();

        // Code quality
        sb.AppendLine("# ================================================");
        sb.AppendLine("# Code Quality");
        sb.AppendLine("# ================================================");
        sb.AppendLine();

        sb.AppendLine("# Unused parameters");
        sb.AppendLine($"dotnet_diagnostic.IDE0060.severity = warning");
        sb.AppendLine();

        sb.AppendLine("# Unused local variables");
        sb.AppendLine($"dotnet_diagnostic.IDE0059.severity = warning");
        sb.AppendLine();

        sb.AppendLine("# Simplify LINQ expressions");
        sb.AppendLine($"dotnet_diagnostic.IDE0120.severity = suggestion");
        sb.AppendLine();

        sb.AppendLine("# Use null propagation");
        sb.AppendLine($"dotnet_diagnostic.IDE0031.severity = suggestion");
        sb.AppendLine();

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string? GenerateRuleset(string outputDir, RuleKeeperConfig? config, bool strict, bool force)
    {
        var path = Path.Combine(outputDir, "RuleKeeper.ruleset");

        if (File.Exists(path) && !force)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping:[/] {path} already exists (use --force to overwrite)");
            return null;
        }

        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<!--");
        sb.AppendLine("  RuleKeeper Analyzer Ruleset");
        sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("  ");
        sb.AppendLine("  To use: Add to your .csproj file:");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <CodeAnalysisRuleSet>RuleKeeper.ruleset</CodeAnalysisRuleSet>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("-->");
        sb.AppendLine("<RuleSet Name=\"RuleKeeper Rules\" Description=\"Code analysis rules enforced by RuleKeeper\" ToolsVersion=\"17.0\">");
        sb.AppendLine();

        // Include all rules
        sb.AppendLine("  <IncludeAll Action=\"Warning\" />");
        sb.AppendLine();

        // Security rules - critical
        var secAction = strict ? "Error" : GetRulesetAction(config, "security", "Error");
        sb.AppendLine("  <!-- Security Rules (CS-SEC-*) -->");
        sb.AppendLine("  <Rules AnalyzerId=\"Microsoft.CodeAnalysis.CSharp\" RuleNamespace=\"Microsoft.CodeAnalysis.CSharp\">");
        sb.AppendLine($"    <Rule Id=\"CA2100\" Action=\"{secAction}\" /> <!-- SQL Injection -->");
        sb.AppendLine($"    <Rule Id=\"CA2104\" Action=\"{secAction}\" /> <!-- Hardcoded credentials -->");
        sb.AppendLine($"    <Rule Id=\"CA3001\" Action=\"{secAction}\" /> <!-- XSS -->");
        sb.AppendLine($"    <Rule Id=\"CA3003\" Action=\"{secAction}\" /> <!-- Path traversal -->");
        sb.AppendLine($"    <Rule Id=\"CA2300\" Action=\"{secAction}\" /> <!-- Insecure deserialization -->");
        sb.AppendLine($"    <Rule Id=\"CA5350\" Action=\"{secAction}\" /> <!-- Weak crypto -->");
        sb.AppendLine($"    <Rule Id=\"CA5351\" Action=\"{secAction}\" /> <!-- Weak crypto -->");
        sb.AppendLine("  </Rules>");
        sb.AppendLine();

        // Async rules
        var asyncAction = strict ? "Error" : GetRulesetAction(config, "async", "Error");
        sb.AppendLine("  <!-- Async Rules (CS-ASYNC-*) -->");
        sb.AppendLine("  <Rules AnalyzerId=\"Microsoft.VisualStudio.Threading.Analyzers\" RuleNamespace=\"Microsoft.VisualStudio.Threading.Analyzers\">");
        sb.AppendLine($"    <Rule Id=\"VSTHRD101\" Action=\"{asyncAction}\" /> <!-- Async void -->");
        sb.AppendLine($"    <Rule Id=\"VSTHRD002\" Action=\"{asyncAction}\" /> <!-- Blocking on async -->");
        sb.AppendLine($"    <Rule Id=\"VSTHRD103\" Action=\"{asyncAction}\" /> <!-- Blocking calls -->");
        sb.AppendLine("  </Rules>");
        sb.AppendLine();

        // Exception rules
        var excAction = strict ? "Error" : GetRulesetAction(config, "exceptions", "Warning");
        sb.AppendLine("  <!-- Exception Rules (CS-EXC-*) -->");
        sb.AppendLine("  <Rules AnalyzerId=\"Microsoft.CodeAnalysis.CSharp\" RuleNamespace=\"Microsoft.CodeAnalysis.CSharp\">");
        sb.AppendLine($"    <Rule Id=\"CA1031\" Action=\"{excAction}\" /> <!-- Don't catch general exceptions -->");
        sb.AppendLine($"    <Rule Id=\"CA2200\" Action=\"{excAction}\" /> <!-- Rethrow to preserve stack -->");
        sb.AppendLine("  </Rules>");
        sb.AppendLine();

        // Naming rules
        var nameAction = strict ? "Error" : GetRulesetAction(config, "naming", "Warning");
        sb.AppendLine("  <!-- Naming Rules (CS-NAME-*) -->");
        sb.AppendLine("  <Rules AnalyzerId=\"Microsoft.CodeAnalysis.CSharp\" RuleNamespace=\"Microsoft.CodeAnalysis.CSharp\">");
        sb.AppendLine($"    <Rule Id=\"IDE1006\" Action=\"{nameAction}\" /> <!-- Naming violations -->");
        sb.AppendLine("  </Rules>");
        sb.AppendLine();

        sb.AppendLine("</RuleSet>");

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string? GenerateDirectoryBuildProps(string outputDir, RuleKeeperConfig? config, bool strict, bool force)
    {
        var path = Path.Combine(outputDir, "Directory.Build.props");

        if (File.Exists(path) && !force)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping:[/] {path} already exists (use --force to overwrite)");
            return null;
        }

        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<!--");
        sb.AppendLine("  RuleKeeper - Directory.Build.props");
        sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("  ");
        sb.AppendLine("  This file applies to all projects in this directory and subdirectories.");
        sb.AppendLine("  Place at solution root for solution-wide enforcement.");
        sb.AppendLine("-->");
        sb.AppendLine("<Project>");
        sb.AppendLine();

        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <!-- Enable code analysis -->");
        sb.AppendLine("    <EnableNETAnalyzers>true</EnableNETAnalyzers>");
        sb.AppendLine("    <AnalysisMode>AllEnabledByDefault</AnalysisMode>");
        sb.AppendLine();
        sb.AppendLine("    <!-- Treat warnings as errors in Release builds -->");
        sb.AppendLine("    <TreatWarningsAsErrors Condition=\"'$(Configuration)' == 'Release'\">true</TreatWarningsAsErrors>");
        sb.AppendLine();
        sb.AppendLine("    <!-- Enable nullable reference types -->");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine();

        if (strict)
        {
            sb.AppendLine("    <!-- Strict mode: All warnings as errors -->");
            sb.AppendLine("    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        }

        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();

        // Recommended analyzer packages
        sb.AppendLine("  <!-- Recommended Analyzer Packages -->");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <!-- Microsoft's code analysis ruleset -->");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.CodeAnalysis.NetAnalyzers\" Version=\"8.*\">");
        sb.AppendLine("      <PrivateAssets>all</PrivateAssets>");
        sb.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>");
        sb.AppendLine("    </PackageReference>");
        sb.AppendLine();
        sb.AppendLine("    <!-- Async/threading analyzers (for CS-ASYNC rules) -->");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.VisualStudio.Threading.Analyzers\" Version=\"17.*\">");
        sb.AppendLine("      <PrivateAssets>all</PrivateAssets>");
        sb.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>");
        sb.AppendLine("    </PackageReference>");
        sb.AppendLine();
        sb.AppendLine("    <!-- Security analyzers (for CS-SEC rules) - Optional -->");
        sb.AppendLine("    <!--");
        sb.AppendLine("    <PackageReference Include=\"SecurityCodeScan.VS2019\" Version=\"5.*\">");
        sb.AppendLine("      <PrivateAssets>all</PrivateAssets>");
        sb.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>");
        sb.AppendLine("    </PackageReference>");
        sb.AppendLine("    -->");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();

        sb.AppendLine("</Project>");

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string GetSeverityString(RuleKeeperConfig? config, string category, string defaultValue)
    {
        if (config == null) return defaultValue;

        if (config.CodingStandards.TryGetValue(category, out var categoryConfig) && categoryConfig.Severity.HasValue)
        {
            return categoryConfig.Severity.Value switch
            {
                SeverityLevel.Critical => "error",
                SeverityLevel.High => "error",
                SeverityLevel.Medium => "warning",
                SeverityLevel.Low => "suggestion",
                SeverityLevel.Info => "suggestion",
                _ => defaultValue
            };
        }

        return defaultValue;
    }

    private static string GetRulesetAction(RuleKeeperConfig? config, string category, string defaultValue)
    {
        if (config == null) return defaultValue;

        if (config.CodingStandards.TryGetValue(category, out var categoryConfig) && categoryConfig.Severity.HasValue)
        {
            return categoryConfig.Severity.Value switch
            {
                SeverityLevel.Critical => "Error",
                SeverityLevel.High => "Error",
                SeverityLevel.Medium => "Warning",
                SeverityLevel.Low => "Info",
                SeverityLevel.Info => "Info",
                _ => defaultValue
            };
        }

        return defaultValue;
    }

    private static void PrintUsageInstructions(string format)
    {
        var panel = new Panel(new Markup(GetUsageText(format)))
        {
            Header = new PanelHeader("[bold blue] How to Use [/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
    }

    private static string GetUsageText(string format)
    {
        var sb = new StringBuilder();

        if (format == "globalconfig" || format == "all")
        {
            sb.AppendLine("[bold].globalconfig[/] (Recommended for .NET 5+):");
            sb.AppendLine("  1. Place the file in your solution root directory");
            sb.AppendLine("  2. The IDE will automatically pick up the rules");
            sb.AppendLine("  3. Build errors/warnings will appear in the Error List");
            sb.AppendLine();
        }

        if (format == "ruleset" || format == "all")
        {
            sb.AppendLine("[bold]RuleKeeper.ruleset[/]:");
            sb.AppendLine("  Add to your .csproj file:");
            sb.AppendLine("  [dim]<PropertyGroup>[/]");
            sb.AppendLine("  [dim]  <CodeAnalysisRuleSet>RuleKeeper.ruleset</CodeAnalysisRuleSet>[/]");
            sb.AppendLine("  [dim]</PropertyGroup>[/]");
            sb.AppendLine();
        }

        if (format == "props" || format == "all")
        {
            sb.AppendLine("[bold]Directory.Build.props[/]:");
            sb.AppendLine("  1. Place at your solution root");
            sb.AppendLine("  2. Applies to all projects in subdirectories");
            sb.AppendLine("  3. Includes recommended analyzer NuGet packages");
            sb.AppendLine();
        }

        sb.AppendLine("[bold yellow]Tip:[/] Combine with [blue]rulekeeper scan[/] in CI/CD for comprehensive enforcement!");

        return sb.ToString();
    }
}
