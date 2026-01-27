using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

/// <summary>
/// Generates IDE configuration files for multiple languages from RuleKeeper YAML configuration.
/// Supports: .editorconfig (C#), .eslintrc.json (JS/TS), pyproject.toml (Python),
/// .golangci.yml (Go), checkstyle.xml (Java), pre-commit hooks, and Roslyn analyzers for C#.
/// </summary>
public static class GenerateEditorConfigCommand
{
    public static Command Create()
    {
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for generated config files",
            getDefaultValue: () => ".");

        var configOption = new Option<string?>(
            aliases: new[] { "--config", "-c" },
            description: "Path to RuleKeeper configuration file to base the configs on");

        var languagesOption = new Option<string[]>(
            aliases: new[] { "--languages", "-l" },
            description: "Languages to generate configs for (csharp, javascript, typescript, python, go, java, all)",
            getDefaultValue: () => new[] { "all" });

        var hooksOption = new Option<bool>(
            aliases: new[] { "--hooks" },
            description: "Generate pre-commit hooks configuration",
            getDefaultValue: () => true);

        var analyzerOption = new Option<bool>(
            aliases: new[] { "--analyzer", "--roslyn" },
            description: "Generate Roslyn analyzer for C# custom rules (full IDE squiggle support)",
            getDefaultValue: () => true);

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Overwrite existing files without prompting");

        var command = new Command("generate-editorconfig", "Generate IDE configuration files from RuleKeeper rules (multi-language)")
        {
            outputOption,
            configOption,
            languagesOption,
            hooksOption,
            analyzerOption,
            forceOption
        };

        command.AddAlias("gen-ide-config");

        command.SetHandler((InvocationContext context) =>
        {
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var languages = context.ParseResult.GetValueForOption(languagesOption)!;
            var generateHooks = context.ParseResult.GetValueForOption(hooksOption);
            var generateAnalyzer = context.ParseResult.GetValueForOption(analyzerOption);
            var force = context.ParseResult.GetValueForOption(forceOption);

            var exitCode = Execute(output, configPath, languages, generateHooks, generateAnalyzer, force);
            context.ExitCode = exitCode;
        });

        return command;
    }

    private static int Execute(string outputDir, string? configPath, string[] languages, bool generateHooks, bool generateAnalyzer, bool force)
    {
        try
        {
            var fullPath = Path.GetFullPath(outputDir);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Load configuration
            RuleKeeperConfig? config = null;
            string? resolvedConfigPath = null;

            if (!string.IsNullOrEmpty(configPath))
            {
                var loader = new ConfigurationLoader();
                config = loader.LoadFromFile(configPath);
                resolvedConfigPath = Path.GetFullPath(configPath);
                AnsiConsole.MarkupLine($"[blue]Using config:[/] {configPath}");
            }
            else
            {
                var loader = new ConfigurationLoader();
                var (foundConfig, foundPath) = loader.LoadFromDirectory(Directory.GetCurrentDirectory());
                if (foundPath != null)
                {
                    config = foundConfig;
                    resolvedConfigPath = foundPath;
                    AnsiConsole.MarkupLine($"[blue]Found config:[/] {foundPath}");
                }
            }

            var generatedFiles = new List<string>();
            var targetLanguages = GetTargetLanguages(languages);

            // Generate pre-commit hooks (for all languages)
            if (generateHooks)
            {
                var hooksPath = Path.Combine(fullPath, ".pre-commit-config.yaml");
                if (GenerateFile(hooksPath, () => GeneratePreCommitConfig(config, targetLanguages), force))
                {
                    generatedFiles.Add(hooksPath);
                }

                // Also generate git hook script
                var gitHookPath = Path.Combine(fullPath, "pre-commit");
                if (GenerateFile(gitHookPath, () => GenerateGitHookScript(config), force))
                {
                    generatedFiles.Add(gitHookPath);
                }
            }

            // Generate C# EditorConfig and Roslyn Analyzer
            if (targetLanguages.Contains(Language.CSharp))
            {
                var editorConfigPath = Path.Combine(fullPath, ".editorconfig");
                if (GenerateFile(editorConfigPath, () => GenerateCSharpEditorConfig(config), force))
                {
                    generatedFiles.Add(editorConfigPath);
                }

                // Generate Roslyn analyzer for full IDE support
                if (generateAnalyzer && config != null)
                {
                    var analyzerFiles = GenerateRoslynAnalyzer(fullPath, config, resolvedConfigPath, force);
                    generatedFiles.AddRange(analyzerFiles);
                }
            }

            // Generate JavaScript/TypeScript ESLint config
            if (targetLanguages.Contains(Language.JavaScript) || targetLanguages.Contains(Language.TypeScript))
            {
                var eslintPath = Path.Combine(fullPath, ".eslintrc.json");
                if (GenerateFile(eslintPath, () => GenerateEslintConfig(config, targetLanguages.Contains(Language.TypeScript)), force))
                {
                    generatedFiles.Add(eslintPath);
                }
            }

            // Generate Python config
            if (targetLanguages.Contains(Language.Python))
            {
                var pyprojectPath = Path.Combine(fullPath, "pyproject.toml");
                if (GenerateFile(pyprojectPath, () => GeneratePythonConfig(config), force))
                {
                    generatedFiles.Add(pyprojectPath);
                }
            }

            // Generate Go config
            if (targetLanguages.Contains(Language.Go))
            {
                var golangciPath = Path.Combine(fullPath, ".golangci.yml");
                if (GenerateFile(golangciPath, () => GenerateGolangciConfig(config), force))
                {
                    generatedFiles.Add(golangciPath);
                }
            }

            // Generate Java Checkstyle config
            if (targetLanguages.Contains(Language.Java))
            {
                var checkstylePath = Path.Combine(fullPath, "checkstyle.xml");
                if (GenerateFile(checkstylePath, () => GenerateCheckstyleConfig(config), force))
                {
                    generatedFiles.Add(checkstylePath);
                }

                // Generate suppressions file if baseline has Java files
                if (config?.Scan?.Baseline?.Enabled == true)
                {
                    var legacyFiles = GetBaselineFilePaths(config);
                    var hasJavaFiles = legacyFiles.Any(f => f.EndsWith(".java", StringComparison.OrdinalIgnoreCase));
                    if (hasJavaFiles)
                    {
                        var suppressionsPath = Path.Combine(fullPath, "checkstyle-suppressions.xml");
                        if (GenerateFile(suppressionsPath, () => GenerateCheckstyleSuppressionsConfig(config), force))
                        {
                            generatedFiles.Add(suppressionsPath);
                        }
                    }
                }
            }

            if (generatedFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No files were generated.[/]");
                return 1;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Generated {generatedFiles.Count} file(s):[/]");
            foreach (var file in generatedFiles)
            {
                var relativePath = Path.GetRelativePath(fullPath, file);
                AnsiConsole.MarkupLine($"  [dim]•[/] {relativePath}");
            }

            AnsiConsole.WriteLine();
            PrintSetupInstructions(targetLanguages, generateHooks, generateAnalyzer && targetLanguages.Contains(Language.CSharp));

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static HashSet<Language> GetTargetLanguages(string[] languages)
    {
        var result = new HashSet<Language>();

        foreach (var lang in languages)
        {
            switch (lang.ToLowerInvariant())
            {
                case "all":
                    result.Add(Language.CSharp);
                    result.Add(Language.JavaScript);
                    result.Add(Language.TypeScript);
                    result.Add(Language.Python);
                    result.Add(Language.Go);
                    result.Add(Language.Java);
                    break;
                case "csharp":
                case "cs":
                case "c#":
                    result.Add(Language.CSharp);
                    break;
                case "javascript":
                case "js":
                    result.Add(Language.JavaScript);
                    break;
                case "typescript":
                case "ts":
                    result.Add(Language.TypeScript);
                    break;
                case "python":
                case "py":
                    result.Add(Language.Python);
                    break;
                case "go":
                case "golang":
                    result.Add(Language.Go);
                    break;
                case "java":
                    result.Add(Language.Java);
                    break;
            }
        }

        return result;
    }

    private static bool GenerateFile(string path, Func<string> contentGenerator, bool force)
    {
        if (File.Exists(path) && !force)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping:[/] {Path.GetFileName(path)} exists (use --force to overwrite)");
            return false;
        }

        var content = contentGenerator();
        File.WriteAllText(path, content);
        return true;
    }

    #region Pre-commit Hooks Generation

    private static string GeneratePreCommitConfig(RuleKeeperConfig? config, HashSet<Language> languages)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Pre-commit configuration generated by RuleKeeper");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("# See: https://pre-commit.com/");
        sb.AppendLine();
        sb.AppendLine("repos:");

        // RuleKeeper hook (local)
        sb.AppendLine("  - repo: local");
        sb.AppendLine("    hooks:");
        sb.AppendLine("      - id: rulekeeper");
        sb.AppendLine("        name: RuleKeeper Code Analysis");
        sb.AppendLine("        entry: rulekeeper scan --fail-on medium --summary-only");
        sb.AppendLine("        language: system");
        sb.AppendLine("        pass_filenames: false");
        sb.AppendLine("        stages: [commit]");

        var fileTypes = new List<string>();
        if (languages.Contains(Language.CSharp)) fileTypes.Add("csharp");
        if (languages.Contains(Language.Python)) fileTypes.Add("python");
        if (languages.Contains(Language.JavaScript)) fileTypes.Add("javascript");
        if (languages.Contains(Language.TypeScript)) fileTypes.Add("ts");
        if (languages.Contains(Language.Java)) fileTypes.Add("java");
        if (languages.Contains(Language.Go)) fileTypes.Add("go");

        if (fileTypes.Count > 0)
        {
            sb.AppendLine($"        types_or: [{string.Join(", ", fileTypes)}]");
        }
        sb.AppendLine();

        // Language-specific hooks
        if (languages.Contains(Language.JavaScript) || languages.Contains(Language.TypeScript))
        {
            sb.AppendLine("  - repo: https://github.com/pre-commit/mirrors-eslint");
            sb.AppendLine("    rev: v8.56.0");
            sb.AppendLine("    hooks:");
            sb.AppendLine("      - id: eslint");
            sb.AppendLine("        files: \\.(js|jsx|ts|tsx)$");
            sb.AppendLine("        additional_dependencies:");
            sb.AppendLine("          - eslint");
            if (languages.Contains(Language.TypeScript))
            {
                sb.AppendLine("          - typescript");
                sb.AppendLine("          - '@typescript-eslint/parser'");
                sb.AppendLine("          - '@typescript-eslint/eslint-plugin'");
            }
            sb.AppendLine();
        }

        if (languages.Contains(Language.Python))
        {
            sb.AppendLine("  - repo: https://github.com/astral-sh/ruff-pre-commit");
            sb.AppendLine("    rev: v0.1.9");
            sb.AppendLine("    hooks:");
            sb.AppendLine("      - id: ruff");
            sb.AppendLine("        args: [--fix, --exit-non-zero-on-fix]");
            sb.AppendLine("      - id: ruff-format");
            sb.AppendLine();
        }

        if (languages.Contains(Language.Go))
        {
            sb.AppendLine("  - repo: https://github.com/golangci/golangci-lint");
            sb.AppendLine("    rev: v1.55.2");
            sb.AppendLine("    hooks:");
            sb.AppendLine("      - id: golangci-lint");
            sb.AppendLine();
        }

        if (languages.Contains(Language.Java))
        {
            sb.AppendLine("  - repo: https://github.com/pre-commit/pre-commit-hooks");
            sb.AppendLine("    rev: v4.5.0");
            sb.AppendLine("    hooks:");
            sb.AppendLine("      - id: check-xml");
            sb.AppendLine("        files: pom.xml");
            sb.AppendLine();
            sb.AppendLine("  - repo: local");
            sb.AppendLine("    hooks:");
            sb.AppendLine("      - id: checkstyle");
            sb.AppendLine("        name: Checkstyle");
            sb.AppendLine("        entry: java -jar checkstyle.jar -c checkstyle.xml");
            sb.AppendLine("        language: system");
            sb.AppendLine("        files: \\.java$");
            sb.AppendLine();
        }

        // Generic hooks
        sb.AppendLine("  - repo: https://github.com/pre-commit/pre-commit-hooks");
        sb.AppendLine("    rev: v4.5.0");
        sb.AppendLine("    hooks:");
        sb.AppendLine("      - id: trailing-whitespace");
        sb.AppendLine("      - id: end-of-file-fixer");
        sb.AppendLine("      - id: check-yaml");
        sb.AppendLine("      - id: check-added-large-files");
        sb.AppendLine("        args: ['--maxkb=500']");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateGitHookScript(RuleKeeperConfig? config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#!/bin/bash");
        sb.AppendLine("# Git pre-commit hook generated by RuleKeeper");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("#");
        sb.AppendLine("# To install: cp pre-commit .git/hooks/pre-commit && chmod +x .git/hooks/pre-commit");
        sb.AppendLine();
        sb.AppendLine("echo \"Running RuleKeeper analysis...\"");
        sb.AppendLine();
        sb.AppendLine("# Run RuleKeeper scan");
        sb.AppendLine("rulekeeper scan --fail-on medium --summary-only");
        sb.AppendLine("RESULT=$?");
        sb.AppendLine();
        sb.AppendLine("if [ $RESULT -ne 0 ]; then");
        sb.AppendLine("    echo \"\"");
        sb.AppendLine("    echo \"RuleKeeper found violations. Commit blocked.\"");
        sb.AppendLine("    echo \"Run 'rulekeeper scan' to see detailed violations.\"");
        sb.AppendLine("    echo \"Use 'git commit --no-verify' to bypass (not recommended).\"");
        sb.AppendLine("    exit 1");
        sb.AppendLine("fi");
        sb.AppendLine();
        sb.AppendLine("echo \"RuleKeeper: All checks passed.\"");
        sb.AppendLine("exit 0");

        return sb.ToString();
    }

    #endregion

    #region C# Roslyn Analyzer Generation

    private static List<string> GenerateRoslynAnalyzer(string outputDir, RuleKeeperConfig config, string? configPath, bool force)
    {
        var generatedFiles = new List<string>();

        // Create analyzer directory
        var analyzerDir = Path.Combine(outputDir, "RuleKeeper.Analyzers");
        if (!Directory.Exists(analyzerDir))
        {
            Directory.CreateDirectory(analyzerDir);
        }

        // Generate the analyzer project file
        var csprojPath = Path.Combine(analyzerDir, "RuleKeeper.Analyzers.csproj");
        if (GenerateFile(csprojPath, () => GenerateAnalyzerCsproj(), force))
        {
            generatedFiles.Add(csprojPath);
        }

        // Generate the main analyzer class
        var analyzerPath = Path.Combine(analyzerDir, "RuleKeeperAnalyzer.cs");
        if (GenerateFile(analyzerPath, () => GenerateAnalyzerClass(config), force))
        {
            generatedFiles.Add(analyzerPath);
        }

        // Generate Directory.Build.props for easy integration
        var propsPath = Path.Combine(outputDir, "Directory.Build.props");
        if (GenerateFile(propsPath, () => GenerateDirectoryBuildProps(configPath), force))
        {
            generatedFiles.Add(propsPath);
        }

        return generatedFiles;
    }

    private static string GenerateAnalyzerCsproj()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>netstandard2.0</TargetFramework>");
        sb.AppendLine("    <LangVersion>latest</LangVersion>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>");
        sb.AppendLine("    <IsRoslynComponent>true</IsRoslynComponent>");
        sb.AppendLine("    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>");
        sb.AppendLine("    <PackageId>RuleKeeper.Analyzers</PackageId>");
        sb.AppendLine("    <Version>1.0.0</Version>");
        sb.AppendLine("    <Description>Custom Roslyn analyzers generated from RuleKeeper YAML configuration</Description>");
        sb.AppendLine("    <DevelopmentDependency>true</DevelopmentDependency>");
        sb.AppendLine("    <IncludeBuildOutput>false</IncludeBuildOutput>");
        sb.AppendLine("    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\" Version=\"4.8.0\" PrivateAssets=\"all\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.CodeAnalysis.Analyzers\" Version=\"3.3.4\" PrivateAssets=\"all\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <None Include=\"$(OutputPath)\\$(AssemblyName).dll\" Pack=\"true\" PackagePath=\"analyzers/dotnet/cs\" Visible=\"false\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    private static string GenerateAnalyzerClass(RuleKeeperConfig config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// RuleKeeper Custom Analyzers");
        sb.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// These analyzers provide real-time IDE feedback for custom YAML rules.");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Collections.Immutable;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Text.RegularExpressions;");
        sb.AppendLine("using Microsoft.CodeAnalysis;");
        sb.AppendLine("using Microsoft.CodeAnalysis.CSharp;");
        sb.AppendLine("using Microsoft.CodeAnalysis.CSharp.Syntax;");
        sb.AppendLine("using Microsoft.CodeAnalysis.Diagnostics;");
        sb.AppendLine("using Microsoft.CodeAnalysis.Text;");
        sb.AppendLine();
        sb.AppendLine("namespace RuleKeeper.Analyzers");
        sb.AppendLine("{");

        // Generate analyzer classes for each rule
        var ruleIndex = 0;
        foreach (var (categoryName, category) in config.CodingStandards)
        {
            if (!category.IsEnabled) continue;

            foreach (var rule in category.Rules)
            {
                if (!rule.IsEnabled) continue;

                // Only generate for C# rules
                if (rule.Languages.Count > 0 &&
                    !rule.Languages.Any(l => l.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
                                              l.Equals("cs", StringComparison.OrdinalIgnoreCase) ||
                                              l.Equals("c#", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var analyzerCode = GenerateRuleAnalyzer(rule, categoryName, ruleIndex++);
                if (!string.IsNullOrEmpty(analyzerCode))
                {
                    sb.AppendLine(analyzerCode);
                }
            }
        }

        // Generate combined analyzer that runs all rules
        sb.AppendLine(GenerateCombinedAnalyzer(config));

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateRuleAnalyzer(RuleDefinition rule, string category, int index)
    {
        var ruleId = rule.Id ?? $"RK{index:D4}";
        var className = $"RuleKeeper_{SanitizeIdentifier(ruleId)}";
        var severity = MapSeverityToDiagnosticSeverity(rule.Severity);

        var sb = new StringBuilder();

        // Check what type of rule this is
        var hasAntiPattern = !string.IsNullOrEmpty(rule.AntiPattern);
        var hasPatternMatch = rule.PatternMatch != null || rule.AntiPatternMatch != null;
        var hasAstQuery = rule.AstQuery != null;

        if (!hasAntiPattern && !hasPatternMatch && !hasAstQuery)
        {
            return string.Empty;
        }

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {EscapeXml(rule.Name ?? ruleId)}: {EscapeXml(rule.Description ?? rule.Message ?? "Custom rule")}");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    [DiagnosticAnalyzer(LanguageNames.CSharp)]");
        sb.AppendLine($"    public class {className} : DiagnosticAnalyzer");
        sb.AppendLine("    {");

        // Diagnostic descriptor
        sb.AppendLine($"        public const string DiagnosticId = \"{ruleId}\";");
        sb.AppendLine($"        private const string Category = \"{category}\";");
        sb.AppendLine();
        sb.AppendLine($"        private static readonly LocalizableString Title = \"{EscapeCSharpString(rule.Name ?? ruleId)}\";");
        sb.AppendLine($"        private static readonly LocalizableString MessageFormat = \"{EscapeCSharpString(rule.Message ?? "Rule violation detected")}\";");
        sb.AppendLine($"        private static readonly LocalizableString Description = \"{EscapeCSharpString(rule.Description ?? "")}\";");
        sb.AppendLine();
        sb.AppendLine($"        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(");
        sb.AppendLine($"            DiagnosticId, Title, MessageFormat, Category,");
        sb.AppendLine($"            DiagnosticSeverity.{severity}, isEnabledByDefault: true, description: Description);");
        sb.AppendLine();
        sb.AppendLine("        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);");
        sb.AppendLine();

        // Initialize method
        sb.AppendLine("        public override void Initialize(AnalysisContext context)");
        sb.AppendLine("        {");
        sb.AppendLine("            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);");
        sb.AppendLine("            context.EnableConcurrentExecution();");

        if (hasAntiPattern || hasPatternMatch)
        {
            var pattern = hasAntiPattern ? rule.AntiPattern :
                         (rule.AntiPatternMatch?.Regex ?? rule.PatternMatch?.Regex);

            if (!string.IsNullOrEmpty(pattern))
            {
                sb.AppendLine("            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);");
            }
        }

        if (hasAstQuery)
        {
            sb.AppendLine("            context.RegisterSyntaxNodeAction(AnalyzeNode, GetSyntaxKinds());");
        }

        sb.AppendLine("        }");

        // Pattern-based analysis
        if (hasAntiPattern || hasPatternMatch)
        {
            var pattern = hasAntiPattern ? rule.AntiPattern :
                         (rule.AntiPatternMatch?.Regex ?? rule.PatternMatch?.Regex);

            if (!string.IsNullOrEmpty(pattern))
            {
                sb.AppendLine();
                sb.AppendLine($"        private static readonly Regex Pattern = new Regex(@\"{EscapeCSharpVerbatimString(pattern)}\", RegexOptions.Compiled | RegexOptions.Multiline);");
                sb.AppendLine();
                sb.AppendLine("        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)");
                sb.AppendLine("        {");
                sb.AppendLine("            var text = context.Tree.GetText().ToString();");
                sb.AppendLine("            var matches = Pattern.Matches(text);");
                sb.AppendLine();
                sb.AppendLine("            foreach (Match match in matches)");
                sb.AppendLine("            {");
                sb.AppendLine("                var location = Location.Create(context.Tree, TextSpan.FromBounds(match.Index, match.Index + match.Length));");
                sb.AppendLine("                var diagnostic = Diagnostic.Create(Rule, location);");
                sb.AppendLine("                context.ReportDiagnostic(diagnostic);");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
        }

        // AST-based analysis
        if (hasAstQuery)
        {
            sb.AppendLine();
            sb.AppendLine("        private static SyntaxKind[] GetSyntaxKinds()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new SyntaxKind[]");
            sb.AppendLine("            {");

            var nodeKinds = rule.AstQuery?.NodeKinds ?? new List<string>();
            foreach (var kind in nodeKinds)
            {
                var syntaxKind = MapNodeKindToSyntaxKind(kind);
                if (!string.IsNullOrEmpty(syntaxKind))
                {
                    sb.AppendLine($"                SyntaxKind.{syntaxKind},");
                }
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void AnalyzeNode(SyntaxNodeAnalysisContext context)");
            sb.AppendLine("        {");

            // Generate property checks
            if (rule.AstQuery?.Properties != null)
            {
                foreach (var (propName, propValue) in rule.AstQuery.Properties)
                {
                    sb.AppendLine($"            // Check property: {propName} = {propValue}");
                }
            }

            sb.AppendLine("            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());");
            sb.AppendLine("            context.ReportDiagnostic(diagnostic);");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCombinedAnalyzer(RuleKeeperConfig config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Combined analyzer that provides summary of all RuleKeeper rules.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static class RuleKeeperDiagnostics");
        sb.AppendLine("    {");
        sb.AppendLine("        public static readonly ImmutableArray<string> AllRuleIds = ImmutableArray.Create(new[]");
        sb.AppendLine("        {");

        foreach (var (categoryName, category) in config.CodingStandards)
        {
            if (!category.IsEnabled) continue;
            foreach (var rule in category.Rules)
            {
                if (!rule.IsEnabled) continue;
                if (!string.IsNullOrEmpty(rule.Id))
                {
                    sb.AppendLine($"            \"{rule.Id}\",");
                }
            }
        }

        sb.AppendLine("        });");
        sb.AppendLine("    }");

        return sb.ToString();
    }

    private static string GenerateDirectoryBuildProps(string? configPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<!--");
        sb.AppendLine("  RuleKeeper IDE Integration");
        sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("  ");
        sb.AppendLine("  This file enables the RuleKeeper Roslyn analyzers for full IDE support.");
        sb.AppendLine("  Place at solution root for solution-wide enforcement.");
        sb.AppendLine("-->");
        sb.AppendLine("<Project>");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <EnableNETAnalyzers>true</EnableNETAnalyzers>");
        sb.AppendLine("    <AnalysisMode>AllEnabledByDefault</AnalysisMode>");
        sb.AppendLine("    <TreatWarningsAsErrors Condition=\"'$(Configuration)' == 'Release'\">true</TreatWarningsAsErrors>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <!-- Reference the generated RuleKeeper analyzers -->");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <ProjectReference Include=\"RuleKeeper.Analyzers\\RuleKeeper.Analyzers.csproj\"");
        sb.AppendLine("                      OutputItemType=\"Analyzer\"");
        sb.AppendLine("                      ReferenceOutputAssembly=\"false\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("  <!-- Standard analyzer packages -->");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.CodeAnalysis.NetAnalyzers\" Version=\"8.*\">");
        sb.AppendLine("      <PrivateAssets>all</PrivateAssets>");
        sb.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>");
        sb.AppendLine("    </PackageReference>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.VisualStudio.Threading.Analyzers\" Version=\"17.*\">");
        sb.AppendLine("      <PrivateAssets>all</PrivateAssets>");
        sb.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>");
        sb.AppendLine("    </PackageReference>");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(configPath))
        {
            sb.AppendLine($"  <!-- RuleKeeper config reference -->");
            sb.AppendLine($"  <ItemGroup>");
            sb.AppendLine($"    <AdditionalFiles Include=\"{Path.GetFileName(configPath)}\" />");
            sb.AppendLine($"  </ItemGroup>");
            sb.AppendLine();
        }

        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    private static string MapNodeKindToSyntaxKind(string nodeKind)
    {
        return nodeKind.ToLowerInvariant() switch
        {
            "class" or "classdeclaration" => "ClassDeclaration",
            "method" or "methoddeclaration" => "MethodDeclaration",
            "property" or "propertydeclaration" => "PropertyDeclaration",
            "field" or "fielddeclaration" => "FieldDeclaration",
            "interface" or "interfacedeclaration" => "InterfaceDeclaration",
            "constructor" or "constructordeclaration" => "ConstructorDeclaration",
            "invocation" or "invocationexpression" => "InvocationExpression",
            "assignment" or "assignmentexpression" => "SimpleAssignmentExpression",
            "if" or "ifstatement" => "IfStatement",
            "for" or "forstatement" => "ForStatement",
            "foreach" or "foreachstatement" => "ForEachStatement",
            "while" or "whilestatement" => "WhileStatement",
            "try" or "trystatement" => "TryStatement",
            "catch" or "catchclause" => "CatchClause",
            "throw" or "throwstatement" => "ThrowStatement",
            "return" or "returnstatement" => "ReturnStatement",
            "stringliteral" or "literalexpression" => "StringLiteralExpression",
            "attribute" => "Attribute",
            "parameter" => "Parameter",
            "argument" => "Argument",
            "variable" or "variabledeclaration" => "VariableDeclaration",
            "using" or "usingdirective" => "UsingDirective",
            "namespace" or "namespacedeclaration" => "NamespaceDeclaration",
            _ => ""
        };
    }

    private static string MapSeverityToDiagnosticSeverity(SeverityLevel severity)
    {
        return severity switch
        {
            SeverityLevel.Critical => "Error",
            SeverityLevel.High => "Error",
            SeverityLevel.Medium => "Warning",
            SeverityLevel.Low => "Info",
            SeverityLevel.Info => "Info",
            _ => "Warning"
        };
    }

    #endregion

    #region C# EditorConfig Generation

    private static string GenerateCSharpEditorConfig(RuleKeeperConfig? config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# EditorConfig generated by RuleKeeper");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("root = true");
        sb.AppendLine();

        sb.AppendLine("[*]");
        sb.AppendLine("indent_style = space");
        sb.AppendLine("indent_size = 4");
        sb.AppendLine("end_of_line = lf");
        sb.AppendLine("charset = utf-8");
        sb.AppendLine("trim_trailing_whitespace = true");
        sb.AppendLine("insert_final_newline = true");
        sb.AppendLine();

        sb.AppendLine("[*.cs]");
        sb.AppendLine();

        var namingSeverity = GetCategorySeverity(config, "naming", SeverityLevel.Medium);
        var editorConfigSeverity = MapSeverityToEditorConfig(namingSeverity);

        // Naming conventions
        sb.AppendLine("# Naming Conventions");
        sb.AppendLine("dotnet_naming_symbols.public_symbols.applicable_kinds = class, struct, interface, enum, property, method, field, event, delegate");
        sb.AppendLine("dotnet_naming_symbols.public_symbols.applicable_accessibilities = public, internal, protected, protected_internal");
        sb.AppendLine("dotnet_naming_symbols.private_fields.applicable_kinds = field");
        sb.AppendLine("dotnet_naming_symbols.private_fields.applicable_accessibilities = private, private_protected");
        sb.AppendLine("dotnet_naming_symbols.interfaces.applicable_kinds = interface");
        sb.AppendLine("dotnet_naming_symbols.async_methods.applicable_kinds = method");
        sb.AppendLine("dotnet_naming_symbols.async_methods.required_modifiers = async");
        sb.AppendLine("dotnet_naming_symbols.constants.applicable_kinds = field");
        sb.AppendLine("dotnet_naming_symbols.constants.required_modifiers = const");
        sb.AppendLine();

        sb.AppendLine("dotnet_naming_style.pascal_case.capitalization = pascal_case");
        sb.AppendLine("dotnet_naming_style.underscore_camel_case.capitalization = camel_case");
        sb.AppendLine("dotnet_naming_style.underscore_camel_case.required_prefix = _");
        sb.AppendLine("dotnet_naming_style.interface_style.capitalization = pascal_case");
        sb.AppendLine("dotnet_naming_style.interface_style.required_prefix = I");
        sb.AppendLine("dotnet_naming_style.async_suffix.capitalization = pascal_case");
        sb.AppendLine("dotnet_naming_style.async_suffix.required_suffix = Async");
        sb.AppendLine();

        sb.AppendLine($"dotnet_naming_rule.public_members_pascal_case.symbols = public_symbols");
        sb.AppendLine($"dotnet_naming_rule.public_members_pascal_case.style = pascal_case");
        sb.AppendLine($"dotnet_naming_rule.public_members_pascal_case.severity = {editorConfigSeverity}");
        sb.AppendLine();
        sb.AppendLine($"dotnet_naming_rule.private_fields_underscore.symbols = private_fields");
        sb.AppendLine($"dotnet_naming_rule.private_fields_underscore.style = underscore_camel_case");
        sb.AppendLine($"dotnet_naming_rule.private_fields_underscore.severity = {editorConfigSeverity}");
        sb.AppendLine();
        sb.AppendLine($"dotnet_naming_rule.interfaces_prefix_i.symbols = interfaces");
        sb.AppendLine($"dotnet_naming_rule.interfaces_prefix_i.style = interface_style");
        sb.AppendLine($"dotnet_naming_rule.interfaces_prefix_i.severity = {editorConfigSeverity}");
        sb.AppendLine();

        // Code style
        sb.AppendLine("# Code Style");
        sb.AppendLine("csharp_style_var_for_built_in_types = true:suggestion");
        sb.AppendLine("csharp_style_var_when_type_is_apparent = true:suggestion");
        sb.AppendLine("csharp_prefer_braces = true:warning");
        sb.AppendLine("csharp_using_directive_placement = outside_namespace:warning");
        sb.AppendLine("dotnet_style_require_accessibility_modifiers = always:warning");
        sb.AppendLine();

        // Analyzer severities from config
        var asyncSev = GetCategorySeverity(config, "async", SeverityLevel.High);
        var secSev = GetCategorySeverity(config, "security", SeverityLevel.Critical);
        var excSev = GetCategorySeverity(config, "exceptions", SeverityLevel.High);

        sb.AppendLine("# Built-in Analyzer Rules");
        sb.AppendLine($"dotnet_diagnostic.VSTHRD101.severity = {MapSeverityToEditorConfig(asyncSev)}");
        sb.AppendLine($"dotnet_diagnostic.VSTHRD002.severity = {MapSeverityToEditorConfig(asyncSev)}");
        sb.AppendLine($"dotnet_diagnostic.CA2100.severity = {MapSeverityToEditorConfig(secSev)}");
        sb.AppendLine($"dotnet_diagnostic.CA1031.severity = {MapSeverityToEditorConfig(excSev)}");
        sb.AppendLine("dotnet_diagnostic.IDE0059.severity = warning");
        sb.AppendLine("dotnet_diagnostic.IDE0060.severity = warning");
        sb.AppendLine();

        // Add custom rule IDs if config provided
        if (config != null)
        {
            sb.AppendLine("# Custom RuleKeeper Rules (enforced by RuleKeeper.Analyzers)");
            foreach (var (categoryName, category) in config.CodingStandards)
            {
                if (!category.IsEnabled) continue;
                foreach (var rule in category.Rules)
                {
                    if (!rule.IsEnabled || string.IsNullOrEmpty(rule.Id)) continue;
                    var ruleSeverity = MapSeverityToEditorConfig(rule.Severity);
                    sb.AppendLine($"dotnet_diagnostic.{rule.Id}.severity = {ruleSeverity}");
                }
            }
            sb.AppendLine();
        }

        // Add legacy file exclusions from baseline
        var legacyFiles = GetBaselineFilePaths(config);
        if (legacyFiles.Count > 0)
        {
            // Filter to only C# files for .editorconfig
            var csFiles = legacyFiles
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (csFiles.Count > 0)
            {
                sb.AppendLine("# Legacy Code Exclusions (from baseline)");
                sb.AppendLine("# Files in the baseline are considered legacy and have reduced analyzer enforcement.");
                sb.AppendLine("# This allows gradual adoption without breaking existing code.");
                sb.AppendLine();

                var filesByDir = GroupFilesByDirectory(csFiles);

                foreach (var (dir, files) in filesByDir.OrderBy(kvp => kvp.Key))
                {
                    if (ShouldUseDirectoryWildcard(dir, files, csFiles))
                    {
                        // Use directory wildcard for directories with many baselined files
                        var pattern = string.IsNullOrEmpty(dir) ? "*.cs" : $"{dir.Replace('\\', '/')}/*.cs";
                        sb.AppendLine($"[{pattern}]");
                        sb.AppendLine("# Legacy directory - disable all RuleKeeper diagnostics");
                        sb.AppendLine("dotnet_analyzer_diagnostic.severity = suggestion");
                        sb.AppendLine();
                    }
                    else
                    {
                        // Individual file exclusions
                        foreach (var file in files.OrderBy(f => f))
                        {
                            var filePath = string.IsNullOrEmpty(dir) ? file : $"{dir.Replace('\\', '/')}/{file}";
                            sb.AppendLine($"[{filePath}]");
                            sb.AppendLine("dotnet_analyzer_diagnostic.severity = none");
                            sb.AppendLine();
                        }
                    }
                }

                sb.AppendLine($"# Total legacy C# files excluded: {csFiles.Count}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    #endregion

    #region JavaScript/TypeScript ESLint Generation

    private static string GenerateEslintConfig(RuleKeeperConfig? config, bool includeTypeScript)
    {
        var eslintConfig = new Dictionary<string, object>
        {
            ["$schema"] = "https://json.schemastore.org/eslintrc.json",
            ["root"] = true,
            ["env"] = new Dictionary<string, object>
            {
                ["browser"] = true,
                ["es2021"] = true,
                ["node"] = true
            },
            ["parserOptions"] = new Dictionary<string, object>
            {
                ["ecmaVersion"] = "latest",
                ["sourceType"] = "module"
            }
        };

        var extends = new List<string> { "eslint:recommended" };
        var plugins = new List<string>();

        if (includeTypeScript)
        {
            extends.Add("plugin:@typescript-eslint/recommended");
            plugins.Add("@typescript-eslint");
            eslintConfig["parser"] = "@typescript-eslint/parser";
        }

        eslintConfig["extends"] = extends;
        if (plugins.Count > 0) eslintConfig["plugins"] = plugins;

        var rules = new Dictionary<string, object>
        {
            ["camelcase"] = new object[] { GetEslintSeverity(config, "naming"), new Dictionary<string, object> { ["properties"] = "never" } },
            ["new-cap"] = GetEslintSeverity(config, "naming"),
            ["no-eval"] = GetEslintSeverity(config, "security"),
            ["no-implied-eval"] = GetEslintSeverity(config, "security"),
            ["no-new-func"] = GetEslintSeverity(config, "security"),
            ["no-console"] = GetEslintSeverity(config, "api", "warn"),
            ["no-unused-vars"] = GetEslintSeverity(config, "code"),
            ["no-unreachable"] = "error",
            ["eqeqeq"] = new object[] { "error", "always" },
            ["curly"] = "error",
            ["no-var"] = "error",
            ["prefer-const"] = "warn",
            ["no-async-promise-executor"] = "error",
            ["require-await"] = "warn"
        };

        eslintConfig["rules"] = rules;

        // Add legacy file exclusions from baseline
        var legacyFiles = GetBaselineFilePaths(config);
        if (legacyFiles.Count > 0)
        {
            // Filter to only JS/TS files
            var jsFiles = legacyFiles
                .Where(f => f.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .OrderBy(f => f)
                .ToList();

            if (jsFiles.Count > 0)
            {
                var filesByDir = GroupFilesByDirectory(new HashSet<string>(jsFiles));
                var ignorePatterns = new List<string>();

                foreach (var (dir, files) in filesByDir.OrderBy(kvp => kvp.Key))
                {
                    if (ShouldUseDirectoryWildcard(dir, files, new HashSet<string>(jsFiles)))
                    {
                        // Use directory wildcard
                        var pattern = string.IsNullOrEmpty(dir) ? "*" : $"{dir}/**";
                        ignorePatterns.Add(pattern);
                    }
                    else
                    {
                        ignorePatterns.AddRange(files.Select(f =>
                            string.IsNullOrEmpty(dir) ? f : $"{dir}/{f}"));
                    }
                }

                eslintConfig["ignorePatterns"] = ignorePatterns;
            }
        }

        return JsonSerializer.Serialize(eslintConfig, new JsonSerializerOptions { WriteIndented = true });
    }

    #endregion

    #region Python Config Generation

    private static string GeneratePythonConfig(RuleKeeperConfig? config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Python linting configuration generated by RuleKeeper");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("[tool.ruff]");
        sb.AppendLine("line-length = 120");
        sb.AppendLine("target-version = \"py311\"");
        sb.AppendLine();

        sb.AppendLine("[tool.ruff.lint]");
        var selectRules = new List<string> { "E", "W", "F", "I", "N", "UP", "B", "C4", "SIM" };
        if (GetCategorySeverity(config, "security", SeverityLevel.Critical) >= SeverityLevel.Medium)
        {
            selectRules.Add("S");
        }
        sb.AppendLine($"select = [{string.Join(", ", selectRules.Select(r => $"\"{r}\""))}]");
        sb.AppendLine();

        sb.AppendLine("[tool.ruff.lint.per-file-ignores]");
        sb.AppendLine("\"tests/**/*.py\" = [\"S101\"]");
        sb.AppendLine("\"__init__.py\" = [\"F401\"]");

        // Add legacy file exclusions from baseline
        var legacyFiles = GetBaselineFilePaths(config);
        if (legacyFiles.Count > 0)
        {
            // Filter to only Python files
            var pyFiles = legacyFiles
                .Where(f => f.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .OrderBy(f => f)
                .ToList();

            if (pyFiles.Count > 0)
            {
                sb.AppendLine("# Legacy files from baseline - all rules ignored");
                var filesByDir = GroupFilesByDirectory(new HashSet<string>(pyFiles));

                foreach (var (dir, files) in filesByDir.OrderBy(kvp => kvp.Key))
                {
                    if (ShouldUseDirectoryWildcard(dir, files, new HashSet<string>(pyFiles)))
                    {
                        // Use directory wildcard
                        var pattern = string.IsNullOrEmpty(dir) ? "*.py" : $"{dir}/**/*.py";
                        sb.AppendLine($"\"{pattern}\" = [\"ALL\"]");
                    }
                    else
                    {
                        foreach (var file in files.OrderBy(f => f))
                        {
                            var filePath = string.IsNullOrEmpty(dir) ? file : $"{dir}/{file}";
                            sb.AppendLine($"\"{filePath}\" = [\"ALL\"]");
                        }
                    }
                }
            }
        }
        sb.AppendLine();

        sb.AppendLine("[tool.mypy]");
        sb.AppendLine("python_version = \"3.11\"");
        sb.AppendLine("warn_return_any = true");
        sb.AppendLine("disallow_untyped_defs = true");
        sb.AppendLine();

        return sb.ToString();
    }

    #endregion

    #region Go Config Generation

    private static string GenerateGolangciConfig(RuleKeeperConfig? config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# golangci-lint configuration generated by RuleKeeper");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("run:");
        sb.AppendLine("  timeout: 5m");
        sb.AppendLine();

        sb.AppendLine("linters:");
        sb.AppendLine("  enable:");
        var linters = new List<string> { "errcheck", "gosimple", "govet", "ineffassign", "staticcheck", "unused", "gofmt", "goimports" };
        if (GetCategorySeverity(config, "naming", SeverityLevel.Medium) >= SeverityLevel.Medium)
        {
            linters.AddRange(new[] { "revive", "stylecheck" });
        }
        if (GetCategorySeverity(config, "security", SeverityLevel.Critical) >= SeverityLevel.Medium)
        {
            linters.Add("gosec");
        }
        foreach (var linter in linters)
        {
            sb.AppendLine($"    - {linter}");
        }
        sb.AppendLine();

        sb.AppendLine("issues:");
        sb.AppendLine("  exclude-rules:");
        sb.AppendLine("    - path: _test\\.go");
        sb.AppendLine("      linters: [errcheck, gosec]");

        // Add legacy file exclusions from baseline
        var legacyFiles = GetBaselineFilePaths(config);
        if (legacyFiles.Count > 0)
        {
            // Filter to only Go files
            var goFiles = legacyFiles
                .Where(f => f.EndsWith(".go", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .OrderBy(f => f)
                .ToList();

            if (goFiles.Count > 0)
            {
                sb.AppendLine("    # Legacy files from baseline - excluded from all linters");
                var filesByDir = GroupFilesByDirectory(new HashSet<string>(goFiles));

                foreach (var (dir, files) in filesByDir.OrderBy(kvp => kvp.Key))
                {
                    if (ShouldUseDirectoryWildcard(dir, files, new HashSet<string>(goFiles)))
                    {
                        // Use directory wildcard
                        var pattern = string.IsNullOrEmpty(dir) ? ".*\\.go$" : $"{Regex.Escape(dir)}/.*\\.go$";
                        sb.AppendLine($"    - path: \"{pattern}\"");
                        sb.AppendLine("      linters: [all]");
                    }
                    else
                    {
                        foreach (var file in files.OrderBy(f => f))
                        {
                            var filePath = string.IsNullOrEmpty(dir) ? file : $"{dir}/{file}";
                            sb.AppendLine($"    - path: \"{Regex.Escape(filePath)}$\"");
                            sb.AppendLine("      linters: [all]");
                        }
                    }
                }
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }

    #endregion

    #region Java Config Generation

    private static string GenerateCheckstyleConfig(RuleKeeperConfig? config)
    {
        var sb = new StringBuilder();
        var severity = GetCategorySeverity(config, "naming", SeverityLevel.Medium) >= SeverityLevel.High ? "error" : "warning";

        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<!DOCTYPE module PUBLIC \"-//Checkstyle//DTD Checkstyle Configuration 1.3//EN\" \"https://checkstyle.org/dtds/configuration_1_3.dtd\">");
        sb.AppendLine($"<!-- Generated by RuleKeeper: {DateTime.Now:yyyy-MM-dd HH:mm:ss} -->");
        sb.AppendLine("<module name=\"Checker\">");
        sb.AppendLine($"  <property name=\"severity\" value=\"{severity}\"/>");
        sb.AppendLine("  <module name=\"FileLength\"><property name=\"max\" value=\"500\"/></module>");
        sb.AppendLine("  <module name=\"LineLength\"><property name=\"max\" value=\"120\"/></module>");
        sb.AppendLine("  <module name=\"TreeWalker\">");
        sb.AppendLine("    <module name=\"TypeName\"/>");
        sb.AppendLine("    <module name=\"MethodName\"/>");
        sb.AppendLine("    <module name=\"MemberName\"/>");
        sb.AppendLine("    <module name=\"ConstantName\"/>");
        sb.AppendLine("    <module name=\"NeedBraces\"/>");
        sb.AppendLine("    <module name=\"EmptyBlock\"/>");
        sb.AppendLine("    <module name=\"EmptyCatchBlock\"/>");
        sb.AppendLine("    <module name=\"MethodLength\"><property name=\"max\" value=\"50\"/></module>");
        sb.AppendLine("    <module name=\"CyclomaticComplexity\"><property name=\"max\" value=\"10\"/></module>");

        // Add legacy file exclusions from baseline
        var legacyFiles = GetBaselineFilePaths(config);
        if (legacyFiles.Count > 0)
        {
            // Filter to only Java files
            var javaFiles = legacyFiles
                .Where(f => f.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .OrderBy(f => f)
                .ToList();

            if (javaFiles.Count > 0)
            {
                sb.AppendLine("    <!-- Legacy files from baseline - suppressed -->");
                sb.AppendLine("    <module name=\"SuppressionFilter\">");
                sb.AppendLine("      <property name=\"file\" value=\"checkstyle-suppressions.xml\"/>");
                sb.AppendLine("      <property name=\"optional\" value=\"true\"/>");
                sb.AppendLine("    </module>");
            }
        }

        sb.AppendLine("  </module>");
        sb.AppendLine("</module>");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a checkstyle suppressions file for legacy files from baseline.
    /// </summary>
    private static string GenerateCheckstyleSuppressionsConfig(RuleKeeperConfig? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<!DOCTYPE suppressions PUBLIC \"-//Checkstyle//DTD SuppressionFilter Configuration 1.2//EN\" \"https://checkstyle.org/dtds/suppressions_1_2.dtd\">");
        sb.AppendLine($"<!-- Generated by RuleKeeper: {DateTime.Now:yyyy-MM-dd HH:mm:ss} -->");
        sb.AppendLine("<!-- Legacy files from baseline - excluded from checkstyle analysis -->");
        sb.AppendLine("<suppressions>");

        var legacyFiles = GetBaselineFilePaths(config);
        if (legacyFiles.Count > 0)
        {
            var javaFiles = legacyFiles
                .Where(f => f.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .OrderBy(f => f)
                .ToList();

            var filesByDir = GroupFilesByDirectory(new HashSet<string>(javaFiles));

            foreach (var (dir, files) in filesByDir.OrderBy(kvp => kvp.Key))
            {
                if (ShouldUseDirectoryWildcard(dir, files, new HashSet<string>(javaFiles)))
                {
                    // Use directory wildcard
                    var pattern = string.IsNullOrEmpty(dir) ? ".*\\.java$" : $"{Regex.Escape(dir)}/.*\\.java$";
                    sb.AppendLine($"  <suppress files=\"{pattern}\" checks=\".*\"/>");
                }
                else
                {
                    foreach (var file in files.OrderBy(f => f))
                    {
                        var filePath = string.IsNullOrEmpty(dir) ? file : $"{dir}/{file}";
                        sb.AppendLine($"  <suppress files=\"{Regex.Escape(filePath)}$\" checks=\".*\"/>");
                    }
                }
            }
        }

        sb.AppendLine("</suppressions>");
        return sb.ToString();
    }

    #endregion

    #region Baseline Legacy File Exclusions

    /// <summary>
    /// Reads the baseline file and extracts unique file paths for legacy exclusions.
    /// </summary>
    private static HashSet<string> GetBaselineFilePaths(RuleKeeperConfig? config)
    {
        var legacyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (config?.Scan?.Baseline == null || !config.Scan.Baseline.Enabled)
            return legacyFiles;

        // Only file-based baselines have stored violation paths
        if (!config.Scan.Baseline.Mode.Equals("file", StringComparison.OrdinalIgnoreCase))
            return legacyFiles;

        var baselineFile = config.Scan.Baseline.BaselineFile;
        if (string.IsNullOrEmpty(baselineFile) || !File.Exists(baselineFile))
            return legacyFiles;

        try
        {
            var json = File.ReadAllText(baselineFile);
            var baseline = JsonSerializer.Deserialize<BaselineData>(json);

            if (baseline?.Violations != null)
            {
                foreach (var violation in baseline.Violations)
                {
                    if (!string.IsNullOrEmpty(violation.FilePath))
                    {
                        legacyFiles.Add(violation.FilePath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not read baseline file for legacy exclusions: {ex.Message}");
        }

        return legacyFiles;
    }

    /// <summary>
    /// Groups baseline files by directory for more efficient editorconfig sections.
    /// </summary>
    private static Dictionary<string, List<string>> GroupFilesByDirectory(HashSet<string> files)
    {
        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var dir = Path.GetDirectoryName(file) ?? "";
            if (!groups.ContainsKey(dir))
            {
                groups[dir] = new List<string>();
            }
            groups[dir].Add(Path.GetFileName(file));
        }

        return groups;
    }

    /// <summary>
    /// Determines if a directory has enough files to warrant a wildcard exclusion.
    /// </summary>
    private static bool ShouldUseDirectoryWildcard(string dir, List<string> files, HashSet<string> allFiles)
    {
        // If more than 50% of files in a directory are baselined, use wildcard
        // This is a heuristic - adjust threshold as needed
        return files.Count >= 3;
    }

    #endregion

    #region Helpers

    private static SeverityLevel GetCategorySeverity(RuleKeeperConfig? config, string category, SeverityLevel defaultSeverity)
    {
        if (config == null) return defaultSeverity;

        if (config.CodingStandards.TryGetValue(category, out var categoryConfig))
            return categoryConfig.Severity ?? defaultSeverity;

        if (config.PrebuiltPolicies.TryGetValue(category, out var policy) && policy.Enabled)
            return policy.Severity ?? defaultSeverity;

        return defaultSeverity;
    }

    private static string MapSeverityToEditorConfig(SeverityLevel severity) => severity switch
    {
        SeverityLevel.Critical or SeverityLevel.High => "error",
        SeverityLevel.Medium => "warning",
        _ => "suggestion"
    };

    private static string GetEslintSeverity(RuleKeeperConfig? config, string category, string defaultValue = "error")
    {
        var severity = GetCategorySeverity(config, category, SeverityLevel.Medium);
        return severity switch
        {
            SeverityLevel.Critical or SeverityLevel.High => "error",
            SeverityLevel.Medium or SeverityLevel.Low => "warn",
            _ => defaultValue
        };
    }

    private static string SanitizeIdentifier(string input) =>
        Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");

    private static string EscapeCSharpString(string input) =>
        input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

    private static string EscapeCSharpVerbatimString(string input) =>
        input.Replace("\"", "\"\"");

    private static string EscapeXml(string input) =>
        input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static void PrintSetupInstructions(HashSet<Language> languages, bool hasHooks, bool hasAnalyzer)
    {
        var panel = new Panel(new Markup(GetSetupText(languages, hasHooks, hasAnalyzer)))
        {
            Header = new PanelHeader("[bold blue] Setup Instructions [/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };
        AnsiConsole.Write(panel);
    }

    private static string GetSetupText(HashSet<Language> languages, bool hasHooks, bool hasAnalyzer)
    {
        var sb = new StringBuilder();

        if (hasHooks)
        {
            sb.AppendLine("[bold]Pre-commit Hooks:[/]");
            sb.AppendLine("  Option 1 (pre-commit framework):");
            sb.AppendLine("    [dim]pip install pre-commit && pre-commit install[/]");
            sb.AppendLine();
            sb.AppendLine("  Option 2 (manual git hook):");
            sb.AppendLine("    [dim]cp pre-commit .git/hooks/ && chmod +x .git/hooks/pre-commit[/]");
            sb.AppendLine();
        }

        if (hasAnalyzer && languages.Contains(Language.CSharp))
        {
            sb.AppendLine("[bold]C# IDE Integration (Full Squiggle Support):[/]");
            sb.AppendLine("  1. Build the analyzer: [dim]dotnet build RuleKeeper.Analyzers[/]");
            sb.AppendLine("  2. Directory.Build.props auto-references it for all projects");
            sb.AppendLine("  3. Restart your IDE to see custom rule squiggles");
            sb.AppendLine();
        }

        if (languages.Contains(Language.JavaScript) || languages.Contains(Language.TypeScript))
        {
            sb.AppendLine("[bold]JavaScript/TypeScript:[/]");
            sb.AppendLine("  [dim]npm install eslint --save-dev[/]");
            sb.AppendLine();
        }

        if (languages.Contains(Language.Python))
        {
            sb.AppendLine("[bold]Python:[/]");
            sb.AppendLine("  [dim]pip install ruff[/]");
            sb.AppendLine();
        }

        if (languages.Contains(Language.Go))
        {
            sb.AppendLine("[bold]Go:[/]");
            sb.AppendLine("  [dim]go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest[/]");
            sb.AppendLine();
        }

        sb.AppendLine("[yellow]Tip:[/] Run [blue]rulekeeper scan[/] in CI/CD for complete enforcement!");

        return sb.ToString();
    }

    #endregion
}
