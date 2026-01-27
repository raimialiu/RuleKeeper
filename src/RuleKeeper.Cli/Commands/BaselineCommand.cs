using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

/// <summary>
/// Commands for managing RuleKeeper baselines for legacy code adoption.
/// </summary>
public static class BaselineCommand
{
    public static Command Create()
    {
        var command = new Command("baseline", "Manage baselines for legacy code adoption");

        command.AddCommand(CreateInitCommand());
        command.AddCommand(CreateStatusCommand());
        command.AddCommand(CreateAddCommand());
        command.AddCommand(CreateRemoveCommand());
        command.AddCommand(CreateRefreshCommand());

        return command;
    }

    #region Init Command

    private static Command CreateInitCommand()
    {
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output path for the baseline file",
            getDefaultValue: () => ".rulekeeper-baseline.json");

        var configOption = new Option<string?>(
            aliases: new[] { "--config", "-c" },
            description: "Path to RuleKeeper configuration file");

        var includeOption = new Option<string[]>(
            aliases: new[] { "--include", "-i" },
            description: "File patterns to include",
            getDefaultValue: () => Array.Empty<string>());

        var excludeOption = new Option<string[]>(
            aliases: new[] { "--exclude", "-e" },
            description: "File patterns to exclude",
            getDefaultValue: () => Array.Empty<string>());

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Overwrite existing baseline file");

        var trackModificationsOption = new Option<bool>(
            aliases: new[] { "--track-modifications" },
            description: "Track file modifications (legacy files get scanned when modified)",
            getDefaultValue: () => true);

        var command = new Command("init", "Initialize a baseline capturing all current files as legacy")
        {
            outputOption,
            configOption,
            includeOption,
            excludeOption,
            forceOption,
            trackModificationsOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var includes = context.ParseResult.GetValueForOption(includeOption)!;
            var excludes = context.ParseResult.GetValueForOption(excludeOption)!;
            var force = context.ParseResult.GetValueForOption(forceOption);
            var trackModifications = context.ParseResult.GetValueForOption(trackModificationsOption);

            context.ExitCode = ExecuteInit(output, configPath, includes, excludes, force, trackModifications);
        });

        return command;
    }

    private static int ExecuteInit(string outputPath, string? configPath, string[] includes, string[] excludes, bool force, bool trackModifications)
    {
        try
        {
            var fullOutputPath = Path.GetFullPath(outputPath);

            if (File.Exists(fullOutputPath) && !force)
            {
                AnsiConsole.MarkupLine($"[yellow]Baseline file already exists:[/] {outputPath}");
                AnsiConsole.MarkupLine("[dim]Use --force to overwrite[/]");
                return 1;
            }

            // Load configuration to get include/exclude patterns
            RuleKeeperConfig? config = null;
            var workingDir = Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(configPath))
            {
                var loader = new ConfigurationLoader();
                config = loader.LoadFromFile(configPath);
                AnsiConsole.MarkupLine($"[blue]Using config:[/] {configPath}");
            }
            else
            {
                var loader = new ConfigurationLoader();
                var (foundConfig, foundPath) = loader.LoadFromDirectory(workingDir);
                if (foundPath != null)
                {
                    config = foundConfig;
                    AnsiConsole.MarkupLine($"[blue]Found config:[/] {foundPath}");
                }
            }

            // Determine include/exclude patterns
            var includePatterns = includes.Length > 0 ? includes.ToList() : GetDefaultIncludePatterns(config);
            var excludePatterns = excludes.Length > 0 ? excludes.ToList() : GetDefaultExcludePatterns(config);

            // Add common excludes
            excludePatterns.AddRange(new[]
            {
                "**/bin/**", "**/obj/**", "**/node_modules/**", "**/.git/**",
                "**/packages/**", "**/dist/**", "**/build/**", "**/.vs/**",
                "**/.idea/**", "**/coverage/**", "**/__pycache__/**"
            });

            AnsiConsole.MarkupLine("[blue]Scanning files...[/]");

            // Collect all files matching patterns
            var files = new List<LegacyFileEntry>();
            var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();

            foreach (var pattern in includePatterns)
            {
                matcher.AddInclude(pattern);
            }
            foreach (var pattern in excludePatterns.Distinct())
            {
                matcher.AddExclude(pattern);
            }

            var result = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(workingDir)));

            foreach (var file in result.Files)
            {
                var fullPath = Path.Combine(workingDir, file.Path);
                var fileInfo = new FileInfo(fullPath);

                if (fileInfo.Exists)
                {
                    var entry = new LegacyFileEntry
                    {
                        Path = file.Path.Replace('\\', '/'),
                        AddedAt = DateTime.UtcNow,
                        LastModified = fileInfo.LastWriteTimeUtc,
                        Size = fileInfo.Length
                    };

                    // Calculate file hash if tracking modifications
                    if (trackModifications)
                    {
                        entry.Hash = CalculateFileHash(fullPath);
                    }

                    files.Add(entry);
                }
            }

            // Create baseline
            var baseline = new LegacyFilesBaseline
            {
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Mode = "legacy_files",
                TrackModifications = trackModifications,
                IncludePatterns = includePatterns,
                ExcludePatterns = excludePatterns.Distinct().ToList(),
                Files = files.OrderBy(f => f.Path).ToList()
            };

            // Write baseline file
            var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            File.WriteAllText(fullOutputPath, json);

            // Display summary
            AnsiConsole.WriteLine();
            var panel = new Panel(new Markup(
                $"[green]Baseline initialized successfully![/]\n\n" +
                $"[blue]Files captured:[/] {files.Count}\n" +
                $"[blue]Track modifications:[/] {trackModifications}\n" +
                $"[blue]Output:[/] {outputPath}\n\n" +
                $"[dim]These files are now marked as legacy and will be skipped during scans.\n" +
                $"New files added to the project will be analyzed.[/]"))
            {
                Header = new PanelHeader("[bold] Baseline Created [/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1)
            };
            AnsiConsole.Write(panel);

            // Show next steps
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Next steps:[/]");
            AnsiConsole.MarkupLine($"  1. Add to your config file:");
            AnsiConsole.MarkupLine($"     [dim]scan:[/]");
            AnsiConsole.MarkupLine($"     [dim]  baseline:[/]");
            AnsiConsole.MarkupLine($"     [dim]    enabled: true[/]");
            AnsiConsole.MarkupLine($"     [dim]    mode: legacy_files[/]");
            AnsiConsole.MarkupLine($"     [dim]    baseline_file: {outputPath}[/]");
            AnsiConsole.MarkupLine($"     [dim]    track_modifications: {trackModifications.ToString().ToLower()}[/]");
            AnsiConsole.MarkupLine($"  2. Commit the baseline file to version control");
            AnsiConsole.MarkupLine($"  3. Run [blue]rulekeeper scan[/] - only new files will be analyzed");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    #endregion

    #region Status Command

    private static Command CreateStatusCommand()
    {
        var baselineOption = new Option<string>(
            aliases: new[] { "--baseline", "-b" },
            description: "Path to baseline file",
            getDefaultValue: () => ".rulekeeper-baseline.json");

        var command = new Command("status", "Show baseline status and statistics")
        {
            baselineOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            var baselinePath = context.ParseResult.GetValueForOption(baselineOption)!;
            context.ExitCode = ExecuteStatus(baselinePath);
        });

        return command;
    }

    private static int ExecuteStatus(string baselinePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(baselinePath);

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[yellow]No baseline file found:[/] {baselinePath}");
                AnsiConsole.MarkupLine("[dim]Run 'rulekeeper baseline init' to create one[/]");
                return 1;
            }

            var json = File.ReadAllText(fullPath);
            var baseline = JsonSerializer.Deserialize<LegacyFilesBaseline>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (baseline == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Could not parse baseline file");
                return 1;
            }

            var workingDir = Directory.GetCurrentDirectory();

            // Check file status
            var existingFiles = 0;
            var missingFiles = 0;
            var modifiedFiles = 0;

            foreach (var file in baseline.Files)
            {
                var filePath = Path.Combine(workingDir, file.Path);
                if (File.Exists(filePath))
                {
                    existingFiles++;
                    if (baseline.TrackModifications && !string.IsNullOrEmpty(file.Hash))
                    {
                        var currentHash = CalculateFileHash(filePath);
                        if (currentHash != file.Hash)
                        {
                            modifiedFiles++;
                        }
                    }
                }
                else
                {
                    missingFiles++;
                }
            }

            // Count new files (not in baseline)
            var newFiles = CountNewFiles(workingDir, baseline);

            // Display status
            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");
            table.Border = TableBorder.Rounded;

            table.AddRow("Baseline file", baselinePath);
            table.AddRow("Created", baseline.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            table.AddRow("Last updated", baseline.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            table.AddRow("Mode", baseline.Mode);
            table.AddRow("Track modifications", baseline.TrackModifications ? "Yes" : "No");
            table.AddRow("", "");
            table.AddRow("[blue]Legacy files[/]", $"[blue]{baseline.Files.Count}[/]");
            table.AddRow("  Still exist", existingFiles.ToString());
            table.AddRow("  Missing/deleted", missingFiles.ToString());
            if (baseline.TrackModifications)
            {
                table.AddRow("  Modified (will be scanned)", $"[yellow]{modifiedFiles}[/]");
            }
            table.AddRow("", "");
            table.AddRow("[green]New files (will be scanned)[/]", $"[green]{newFiles}[/]");

            AnsiConsole.Write(table);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static int CountNewFiles(string workingDir, LegacyFilesBaseline baseline)
    {
        var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();

        foreach (var pattern in baseline.IncludePatterns)
        {
            matcher.AddInclude(pattern);
        }
        foreach (var pattern in baseline.ExcludePatterns)
        {
            matcher.AddExclude(pattern);
        }

        var result = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(workingDir)));
        var baselineFiles = new HashSet<string>(baseline.Files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);

        return result.Files.Count(f => !baselineFiles.Contains(f.Path.Replace('\\', '/')));
    }

    #endregion

    #region Add Command

    private static Command CreateAddCommand()
    {
        var filesArgument = new Argument<string[]>(
            name: "files",
            description: "Files or patterns to add to the baseline");

        var baselineOption = new Option<string>(
            aliases: new[] { "--baseline", "-b" },
            description: "Path to baseline file",
            getDefaultValue: () => ".rulekeeper-baseline.json");

        var command = new Command("add", "Add files to the baseline (mark as legacy)")
        {
            filesArgument,
            baselineOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            var files = context.ParseResult.GetValueForArgument(filesArgument);
            var baselinePath = context.ParseResult.GetValueForOption(baselineOption)!;
            context.ExitCode = ExecuteAdd(files, baselinePath);
        });

        return command;
    }

    private static int ExecuteAdd(string[] files, string baselinePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(baselinePath);

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Baseline file not found: {baselinePath}");
                AnsiConsole.MarkupLine("[dim]Run 'rulekeeper baseline init' first[/]");
                return 1;
            }

            var json = File.ReadAllText(fullPath);
            var baseline = JsonSerializer.Deserialize<LegacyFilesBaseline>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (baseline == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Could not parse baseline file");
                return 1;
            }

            var workingDir = Directory.GetCurrentDirectory();
            var existingPaths = new HashSet<string>(baseline.Files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);
            var addedCount = 0;

            foreach (var filePattern in files)
            {
                // Check if it's a glob pattern or a specific file
                if (filePattern.Contains('*') || filePattern.Contains('?'))
                {
                    var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();
                    matcher.AddInclude(filePattern);
                    var result = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(workingDir)));

                    foreach (var match in result.Files)
                    {
                        var relativePath = match.Path.Replace('\\', '/');
                        if (!existingPaths.Contains(relativePath))
                        {
                            var entry = CreateFileEntry(workingDir, relativePath, baseline.TrackModifications);
                            if (entry != null)
                            {
                                baseline.Files.Add(entry);
                                existingPaths.Add(relativePath);
                                addedCount++;
                                AnsiConsole.MarkupLine($"[green]+[/] {relativePath}");
                            }
                        }
                    }
                }
                else
                {
                    var relativePath = Path.GetRelativePath(workingDir, Path.GetFullPath(filePattern)).Replace('\\', '/');
                    if (!existingPaths.Contains(relativePath))
                    {
                        var entry = CreateFileEntry(workingDir, relativePath, baseline.TrackModifications);
                        if (entry != null)
                        {
                            baseline.Files.Add(entry);
                            existingPaths.Add(relativePath);
                            addedCount++;
                            AnsiConsole.MarkupLine($"[green]+[/] {relativePath}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]![/] File not found: {relativePath}");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[dim]=[/] Already in baseline: {relativePath}");
                    }
                }
            }

            if (addedCount > 0)
            {
                baseline.UpdatedAt = DateTime.UtcNow;
                baseline.Files = baseline.Files.OrderBy(f => f.Path).ToList();

                var updatedJson = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                File.WriteAllText(fullPath, updatedJson);

                AnsiConsole.MarkupLine($"\n[green]Added {addedCount} file(s) to baseline[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("\n[yellow]No new files added[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    #endregion

    #region Remove Command

    private static Command CreateRemoveCommand()
    {
        var filesArgument = new Argument<string[]>(
            name: "files",
            description: "Files or patterns to remove from the baseline");

        var baselineOption = new Option<string>(
            aliases: new[] { "--baseline", "-b" },
            description: "Path to baseline file",
            getDefaultValue: () => ".rulekeeper-baseline.json");

        var command = new Command("remove", "Remove files from the baseline (start tracking them)")
        {
            filesArgument,
            baselineOption
        };

        command.AddAlias("rm");

        command.SetHandler((InvocationContext context) =>
        {
            var files = context.ParseResult.GetValueForArgument(filesArgument);
            var baselinePath = context.ParseResult.GetValueForOption(baselineOption)!;
            context.ExitCode = ExecuteRemove(files, baselinePath);
        });

        return command;
    }

    private static int ExecuteRemove(string[] files, string baselinePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(baselinePath);

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Baseline file not found: {baselinePath}");
                return 1;
            }

            var json = File.ReadAllText(fullPath);
            var baseline = JsonSerializer.Deserialize<LegacyFilesBaseline>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (baseline == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Could not parse baseline file");
                return 1;
            }

            var removedCount = 0;
            var filesToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePattern in files)
            {
                if (filePattern.Contains('*') || filePattern.Contains('?'))
                {
                    // Convert glob pattern to regex for matching
                    var regexPattern = "^" + Regex.Escape(filePattern)
                        .Replace("\\*\\*", ".*")
                        .Replace("\\*", "[^/]*")
                        .Replace("\\?", ".") + "$";
                    var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                    foreach (var entry in baseline.Files)
                    {
                        if (regex.IsMatch(entry.Path))
                        {
                            filesToRemove.Add(entry.Path);
                        }
                    }
                }
                else
                {
                    var normalizedPath = filePattern.Replace('\\', '/');
                    filesToRemove.Add(normalizedPath);
                }
            }

            var originalCount = baseline.Files.Count;
            baseline.Files = baseline.Files.Where(f => !filesToRemove.Contains(f.Path)).ToList();
            removedCount = originalCount - baseline.Files.Count;

            foreach (var removed in filesToRemove)
            {
                if (baseline.Files.All(f => !f.Path.Equals(removed, StringComparison.OrdinalIgnoreCase)))
                {
                    AnsiConsole.MarkupLine($"[red]-[/] {removed}");
                }
            }

            if (removedCount > 0)
            {
                baseline.UpdatedAt = DateTime.UtcNow;

                var updatedJson = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                File.WriteAllText(fullPath, updatedJson);

                AnsiConsole.MarkupLine($"\n[green]Removed {removedCount} file(s) from baseline[/]");
                AnsiConsole.MarkupLine("[dim]These files will now be analyzed during scans[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("\n[yellow]No files removed[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    #endregion

    #region Refresh Command

    private static Command CreateRefreshCommand()
    {
        var baselineOption = new Option<string>(
            aliases: new[] { "--baseline", "-b" },
            description: "Path to baseline file",
            getDefaultValue: () => ".rulekeeper-baseline.json");

        var removeDeletedOption = new Option<bool>(
            aliases: new[] { "--remove-deleted" },
            description: "Remove entries for files that no longer exist",
            getDefaultValue: () => false);

        var updateHashesOption = new Option<bool>(
            aliases: new[] { "--update-hashes" },
            description: "Update file hashes (resets modification tracking)",
            getDefaultValue: () => false);

        var command = new Command("refresh", "Refresh baseline file metadata")
        {
            baselineOption,
            removeDeletedOption,
            updateHashesOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            var baselinePath = context.ParseResult.GetValueForOption(baselineOption)!;
            var removeDeleted = context.ParseResult.GetValueForOption(removeDeletedOption);
            var updateHashes = context.ParseResult.GetValueForOption(updateHashesOption);
            context.ExitCode = ExecuteRefresh(baselinePath, removeDeleted, updateHashes);
        });

        return command;
    }

    private static int ExecuteRefresh(string baselinePath, bool removeDeleted, bool updateHashes)
    {
        try
        {
            var fullPath = Path.GetFullPath(baselinePath);

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Baseline file not found: {baselinePath}");
                return 1;
            }

            var json = File.ReadAllText(fullPath);
            var baseline = JsonSerializer.Deserialize<LegacyFilesBaseline>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (baseline == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Could not parse baseline file");
                return 1;
            }

            var workingDir = Directory.GetCurrentDirectory();
            var removedCount = 0;
            var updatedCount = 0;

            var updatedFiles = new List<LegacyFileEntry>();

            foreach (var entry in baseline.Files)
            {
                var filePath = Path.Combine(workingDir, entry.Path);

                if (!File.Exists(filePath))
                {
                    if (removeDeleted)
                    {
                        AnsiConsole.MarkupLine($"[red]-[/] {entry.Path} [dim](deleted)[/]");
                        removedCount++;
                        continue;
                    }
                }
                else if (updateHashes && baseline.TrackModifications)
                {
                    var fileInfo = new FileInfo(filePath);
                    var newHash = CalculateFileHash(filePath);

                    if (entry.Hash != newHash)
                    {
                        entry.Hash = newHash;
                        entry.LastModified = fileInfo.LastWriteTimeUtc;
                        entry.Size = fileInfo.Length;
                        updatedCount++;
                        AnsiConsole.MarkupLine($"[blue]~[/] {entry.Path} [dim](hash updated)[/]");
                    }
                }

                updatedFiles.Add(entry);
            }

            baseline.Files = updatedFiles;
            baseline.UpdatedAt = DateTime.UtcNow;

            var updatedJson = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            File.WriteAllText(fullPath, updatedJson);

            AnsiConsole.WriteLine();
            if (removedCount > 0)
                AnsiConsole.MarkupLine($"[green]Removed {removedCount} deleted file(s)[/]");
            if (updatedCount > 0)
                AnsiConsole.MarkupLine($"[green]Updated {updatedCount} file hash(es)[/]");
            if (removedCount == 0 && updatedCount == 0)
                AnsiConsole.MarkupLine("[dim]No changes made[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    #endregion

    #region Helpers

    private static List<string> GetDefaultIncludePatterns(RuleKeeperConfig? config)
    {
        if (config != null)
        {
            return config.Scan.GetAllIncludePatterns();
        }

        // Default patterns for common languages
        return new List<string>
        {
            "**/*.cs", "**/*.js", "**/*.ts", "**/*.jsx", "**/*.tsx",
            "**/*.py", "**/*.go", "**/*.java"
        };
    }

    private static List<string> GetDefaultExcludePatterns(RuleKeeperConfig? config)
    {
        if (config != null)
        {
            return config.Scan.GetAllExcludePatterns();
        }

        return new List<string>();
    }

    private static string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }

    private static LegacyFileEntry? CreateFileEntry(string workingDir, string relativePath, bool calculateHash)
    {
        var fullPath = Path.Combine(workingDir, relativePath);
        if (!File.Exists(fullPath))
            return null;

        var fileInfo = new FileInfo(fullPath);
        var entry = new LegacyFileEntry
        {
            Path = relativePath,
            AddedAt = DateTime.UtcNow,
            LastModified = fileInfo.LastWriteTimeUtc,
            Size = fileInfo.Length
        };

        if (calculateHash)
        {
            entry.Hash = CalculateFileHash(fullPath);
        }

        return entry;
    }

    #endregion
}

#region Data Models

/// <summary>
/// Baseline data for legacy files adoption mode.
/// </summary>
public class LegacyFilesBaseline
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Mode { get; set; } = "legacy_files";
    public bool TrackModifications { get; set; } = true;
    public List<string> IncludePatterns { get; set; } = new();
    public List<string> ExcludePatterns { get; set; } = new();
    public List<LegacyFileEntry> Files { get; set; } = new();
}

/// <summary>
/// Entry for a single legacy file in the baseline.
/// </summary>
public class LegacyFileEntry
{
    public string Path { get; set; } = "";
    public DateTime AddedAt { get; set; }
    public DateTime LastModified { get; set; }
    public long Size { get; set; }
    public string? Hash { get; set; }
}

#endregion
