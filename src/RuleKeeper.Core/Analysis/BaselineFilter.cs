using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Analysis;

/// <summary>
/// Filters files and violations based on baseline configuration.
/// Supports git-based, file-based, date-based, and legacy_files incremental scanning.
/// </summary>
public class BaselineFilter
{
    private readonly BaselineConfig _config;
    private readonly string _workingDirectory;
    private HashSet<string>? _changedFiles;
    private Dictionary<string, HashSet<int>>? _changedLinesByFile;
    private HashSet<string>? _baselineViolationKeys;
    private LegacyFilesBaseline? _legacyFilesBaseline;
    private HashSet<string>? _legacyFiles;
    private Dictionary<string, string>? _legacyFileHashes;

    public BaselineFilter(BaselineConfig config, string workingDirectory)
    {
        _config = config;
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Initialize the baseline filter by loading changed files/lines.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
            return;

        switch (_config.Mode.ToLowerInvariant())
        {
            case "git":
                await InitializeGitBaselineAsync(cancellationToken);
                break;
            case "file":
                await InitializeFileBaselineAsync(cancellationToken);
                break;
            case "date":
                await InitializeDateBaselineAsync(cancellationToken);
                break;
            case "legacy_files":
                await InitializeLegacyFilesBaselineAsync(cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unknown baseline mode: {_config.Mode}");
        }
    }

    /// <summary>
    /// Check if a file should be scanned based on baseline.
    /// </summary>
    public bool ShouldScanFile(string filePath)
    {
        if (!_config.Enabled)
            return true;

        // Handle legacy_files mode
        if (_config.Mode.Equals("legacy_files", StringComparison.OrdinalIgnoreCase) && _legacyFiles != null)
        {
            var relativePath = GetRelativePath(filePath).Replace('\\', '/');

            // If file is not in legacy list, scan it (it's a new file)
            if (!_legacyFiles.Contains(relativePath))
                return true;

            // If tracking modifications, check if file has been modified
            if (_config.TrackModifications && _legacyFileHashes != null)
            {
                if (_legacyFileHashes.TryGetValue(relativePath, out var originalHash))
                {
                    var currentHash = CalculateFileHash(filePath);
                    // If hash changed, the file has been modified - scan it
                    return currentHash != originalHash;
                }
            }

            // File is legacy and not modified (or not tracking modifications) - skip it
            return false;
        }

        // Handle other modes (git, date) that use _changedFiles
        if (_changedFiles == null)
            return true;

        var relPath = GetRelativePath(filePath);
        return _changedFiles.Contains(relPath) ||
               _changedFiles.Contains(filePath) ||
               _changedFiles.Any(f => filePath.EndsWith(f, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filter violations to only those on changed lines (when filter_to_diff is enabled).
    /// </summary>
    public List<Violation> FilterViolations(List<Violation> violations)
    {
        if (!_config.Enabled)
            return violations;

        // If using file baseline, filter against stored violations
        if (_config.Mode.Equals("file", StringComparison.OrdinalIgnoreCase) && _baselineViolationKeys != null)
        {
            return violations.Where(v => !IsInBaseline(v)).ToList();
        }

        // If not filtering to diff, return all violations from changed files
        if (!_config.FilterToDiff || _changedLinesByFile == null)
            return violations;

        // Filter to only violations on changed lines
        return violations.Where(v =>
        {
            var relativePath = GetRelativePath(v.FilePath);
            if (_changedLinesByFile.TryGetValue(relativePath, out var changedLines))
            {
                // Check if violation line is in changed lines
                return changedLines.Contains(v.StartLine) ||
                       Enumerable.Range(v.StartLine, Math.Max(1, v.EndLine - v.StartLine + 1))
                           .Any(line => changedLines.Contains(line));
            }
            // If we don't have line info, include violation if file is in changed files
            return _changedFiles?.Contains(relativePath) == true;
        }).ToList();
    }

    /// <summary>
    /// Get the list of changed files for reporting.
    /// </summary>
    public IReadOnlyCollection<string> GetChangedFiles()
    {
        return _changedFiles?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Save current violations as baseline (when auto_update is enabled).
    /// </summary>
    public async Task SaveBaselineAsync(List<Violation> violations, CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled || !_config.AutoUpdate || string.IsNullOrEmpty(_config.BaselineFile))
            return;

        var baseline = new BaselineData
        {
            GeneratedAt = DateTime.UtcNow,
            GitRef = _config.GitRef,
            Violations = violations.Select(v => new BaselineViolation
            {
                RuleId = v.RuleId,
                FilePath = GetRelativePath(v.FilePath),
                StartLine = v.StartLine,
                Message = v.Message
            }).ToList()
        };

        var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_config.BaselineFile, json, cancellationToken);
    }

    private async Task InitializeLegacyFilesBaselineAsync(CancellationToken cancellationToken)
    {
        _legacyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _legacyFileHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(_config.BaselineFile))
        {
            HandleMissingBaseline("No baseline file specified for legacy_files mode");
            return;
        }

        if (!File.Exists(_config.BaselineFile))
        {
            HandleMissingBaseline($"Legacy files baseline not found: {_config.BaselineFile}");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_config.BaselineFile, cancellationToken);
            _legacyFilesBaseline = JsonSerializer.Deserialize<LegacyFilesBaseline>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (_legacyFilesBaseline?.Files != null)
            {
                foreach (var file in _legacyFilesBaseline.Files)
                {
                    _legacyFiles.Add(file.Path);
                    if (!string.IsNullOrEmpty(file.Hash))
                    {
                        _legacyFileHashes[file.Path] = file.Hash;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleMissingBaseline($"Failed to load legacy files baseline: {ex.Message}");
        }
    }

    private async Task InitializeGitBaselineAsync(CancellationToken cancellationToken)
    {
        _changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _changedLinesByFile = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        var gitRef = _config.GitRef ?? "HEAD~1";

        // Get list of changed files between git_ref and HEAD
        var (exitCode, output) = await RunGitCommandAsync($"git diff --name-only {gitRef} HEAD", cancellationToken);
        if (exitCode == 0)
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                _changedFiles.Add(line.Trim());
            }
        }

        // Get uncommitted changes (staged + unstaged) if configured
        if (_config.IncludeUncommitted)
        {
            var (uncommittedExit, uncommittedOutput) = await RunGitCommandAsync("git diff --name-only", cancellationToken);
            if (uncommittedExit == 0)
            {
                foreach (var line in uncommittedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    _changedFiles.Add(line.Trim());
                }
            }

            // Also get staged changes
            var (stagedExit, stagedOutput) = await RunGitCommandAsync("git diff --name-only --cached", cancellationToken);
            if (stagedExit == 0)
            {
                foreach (var line in stagedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    _changedFiles.Add(line.Trim());
                }
            }
        }

        // Get untracked files if configured
        if (_config.IncludeUntracked)
        {
            var (untrackedExit, untrackedOutput) = await RunGitCommandAsync("git ls-files --others --exclude-standard", cancellationToken);
            if (untrackedExit == 0)
            {
                foreach (var line in untrackedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    _changedFiles.Add(line.Trim());
                }
            }
        }

        // If filtering to diff, get changed lines for each file
        if (_config.FilterToDiff || _config.ChangedLinesOnly)
        {
            foreach (var file in _changedFiles.ToList())
            {
                var lineCommand = $"git diff {gitRef} --unified=0 -- \"{file}\"";
                var (lineExit, lineOutput) = await RunGitCommandAsync(lineCommand, cancellationToken);
                if (lineExit == 0)
                {
                    var changedLines = ParseDiffForChangedLines(lineOutput);
                    if (changedLines.Count > 0)
                    {
                        _changedLinesByFile[file] = changedLines;
                    }
                }
            }
        }
    }

    private async Task InitializeFileBaselineAsync(CancellationToken cancellationToken)
    {
        _baselineViolationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(_config.BaselineFile))
        {
            HandleMissingBaseline("No baseline file specified");
            return;
        }

        if (!File.Exists(_config.BaselineFile))
        {
            HandleMissingBaseline($"Baseline file not found: {_config.BaselineFile}");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_config.BaselineFile, cancellationToken);
            var baseline = JsonSerializer.Deserialize<BaselineData>(json);
            if (baseline?.Violations != null)
            {
                foreach (var v in baseline.Violations)
                {
                    _baselineViolationKeys.Add(GetViolationKey(v.RuleId, v.FilePath, v.StartLine));
                }
            }
        }
        catch (Exception ex)
        {
            HandleMissingBaseline($"Failed to load baseline: {ex.Message}");
        }

        // For file mode, we scan all files but filter violations
        _changedFiles = null;
    }

    private async Task InitializeDateBaselineAsync(CancellationToken cancellationToken)
    {
        _changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(_config.SinceDate))
        {
            HandleMissingBaseline("No since_date specified for date baseline mode");
            return;
        }

        if (!DateTime.TryParse(_config.SinceDate, out var sinceDate))
        {
            HandleMissingBaseline($"Invalid date format: {_config.SinceDate}");
            return;
        }

        // Find files modified since the date
        var directory = new DirectoryInfo(_workingDirectory);
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            if (file.LastWriteTimeUtc >= sinceDate)
            {
                var relativePath = Path.GetRelativePath(_workingDirectory, file.FullName);
                _changedFiles.Add(relativePath);
            }
        }
    }

    private void HandleMissingBaseline(string message)
    {
        switch (_config.OnMissing.ToLowerInvariant())
        {
            case "fail":
                throw new InvalidOperationException($"Baseline error: {message}");
            case "warn":
                Console.Error.WriteLine($"Warning: {message}. Scanning all files.");
                _changedFiles = null;
                break;
            case "ignore":
            default:
                _changedFiles = null;
                break;
        }
    }

    private bool IsInBaseline(Violation violation)
    {
        if (_baselineViolationKeys == null)
            return false;

        var key = GetViolationKey(violation.RuleId, GetRelativePath(violation.FilePath), violation.StartLine);
        return _baselineViolationKeys.Contains(key);
    }

    private static string GetViolationKey(string ruleId, string filePath, int line)
    {
        return $"{ruleId}|{filePath}|{line}";
    }

    private string GetRelativePath(string filePath)
    {
        try
        {
            return Path.GetRelativePath(_workingDirectory, filePath);
        }
        catch
        {
            return filePath;
        }
    }

    private async Task<(int exitCode, string output)> RunGitCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command.Replace("git ", ""),
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return (-1, "");

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, "");
        }
    }

    private static HashSet<int> ParseDiffForChangedLines(string diffOutput)
    {
        var changedLines = new HashSet<int>();

        foreach (var line in diffOutput.Split('\n'))
        {
            // Parse @@ -a,b +c,d @@ format
            if (line.StartsWith("@@"))
            {
                var parts = line.Split(' ');
                if (parts.Length >= 3)
                {
                    var newRange = parts[2]; // +c,d
                    if (newRange.StartsWith("+"))
                    {
                        var rangeParts = newRange.Substring(1).Split(',');
                        if (int.TryParse(rangeParts[0], out var startLine))
                        {
                            var lineCount = rangeParts.Length > 1 && int.TryParse(rangeParts[1], out var count) ? count : 1;
                            for (var i = 0; i < lineCount; i++)
                            {
                                changedLines.Add(startLine + i);
                            }
                        }
                    }
                }
            }
        }

        return changedLines;
    }

    private static string CalculateFileHash(string filePath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the count of legacy files being skipped.
    /// </summary>
    public int GetLegacyFilesCount() => _legacyFiles?.Count ?? 0;

    /// <summary>
    /// Returns true if using legacy_files mode.
    /// </summary>
    public bool IsLegacyFilesMode => _config.Mode.Equals("legacy_files", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Data structure for baseline file.
/// </summary>
public class BaselineData
{
    public DateTime GeneratedAt { get; set; }
    public string? GitRef { get; set; }
    public List<BaselineViolation> Violations { get; set; } = new();
}

/// <summary>
/// A violation stored in the baseline.
/// </summary>
public class BaselineViolation
{
    public string RuleId { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int StartLine { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Baseline data for legacy files adoption mode.
/// Used to track existing files that should be skipped during analysis.
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
