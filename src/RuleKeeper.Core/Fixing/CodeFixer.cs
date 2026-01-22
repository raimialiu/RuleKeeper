using System.Diagnostics;
using System.Text;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Fixing;

// Re-export SDK types for convenience
using IFixProvider = RuleKeeper.Sdk.IFixProvider;
using CodeFix = RuleKeeper.Sdk.CodeFix;

/// <summary>
/// Engine for applying code fixes to resolve violations.
/// </summary>
public class CodeFixer
{
    private readonly List<IFixProvider> _fixProviders = new();
    private readonly bool _createBackups;
    private readonly bool _dryRun;

    public CodeFixer(bool createBackups = true, bool dryRun = false)
    {
        _createBackups = createBackups;
        _dryRun = dryRun;
    }

    /// <summary>
    /// Registers a fix provider.
    /// </summary>
    public void RegisterProvider(IFixProvider provider)
    {
        _fixProviders.Add(provider);
    }

    /// <summary>
    /// Gets all available fixes for the given violations.
    /// </summary>
    public List<CodeFix> GetAvailableFixes(IEnumerable<Violation> violations, string? ruleIdFilter = null, string? categoryFilter = null)
    {
        var fixes = new List<CodeFix>();

        foreach (var violation in violations)
        {
            // Apply filters
            if (!string.IsNullOrEmpty(ruleIdFilter) &&
                !violation.RuleId.Equals(ruleIdFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(categoryFilter) &&
                !violation.RuleId.Contains($"-{categoryFilter.ToUpper()}-", StringComparison.OrdinalIgnoreCase) &&
                !violation.RuleId.StartsWith($"CS-{categoryFilter.ToUpper()}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Find providers that can fix this violation
            foreach (var provider in _fixProviders)
            {
                if (provider.CanFix(violation))
                {
                    try
                    {
                        var sourceCode = File.ReadAllText(violation.FilePath);
                        var providerFixes = provider.GetFixes(violation, sourceCode);
                        fixes.AddRange(providerFixes);
                    }
                    catch
                    {
                        // Skip if file can't be read
                    }
                }
            }
        }

        return fixes;
    }

    /// <summary>
    /// Applies fixes to resolve violations.
    /// </summary>
    public async Task<FixSummary> ApplyFixesAsync(
        IEnumerable<Violation> violations,
        string? ruleIdFilter = null,
        string? categoryFilter = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var summary = new FixSummary();

        // Get all available fixes
        var fixes = GetAvailableFixes(violations, ruleIdFilter, categoryFilter);

        // Group fixes by file for efficient application
        var fixesByFile = fixes
            .GroupBy(f => f.FilePath)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.StartLine).ThenByDescending(f => f.StartColumn).ToList());

        var modifiedFiles = new HashSet<string>();

        foreach (var (filePath, fileFixes) in fixesByFile)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var results = await ApplyFixesToFileAsync(filePath, fileFixes, cancellationToken);
                summary.Results.AddRange(results);

                foreach (var result in results)
                {
                    summary.TotalFixesAttempted++;
                    if (result.Success)
                    {
                        summary.SuccessfulFixes++;
                        modifiedFiles.Add(filePath);

                        // Track by rule
                        if (!summary.FixesByRule.ContainsKey(result.Fix.RuleId))
                            summary.FixesByRule[result.Fix.RuleId] = 0;
                        summary.FixesByRule[result.Fix.RuleId]++;

                        // Track by category
                        if (!summary.FixesByCategory.ContainsKey(result.Fix.Category))
                            summary.FixesByCategory[result.Fix.Category] = 0;
                        summary.FixesByCategory[result.Fix.Category]++;
                    }
                    else
                    {
                        summary.FailedFixes++;
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var fix in fileFixes)
                {
                    summary.Results.Add(FixResult.Failed(fix, filePath, ex.Message));
                    summary.TotalFixesAttempted++;
                    summary.FailedFixes++;
                }
            }
        }

        summary.FilesModified = modifiedFiles.Count;
        summary.Duration = stopwatch.Elapsed;

        return summary;
    }

    private async Task<List<FixResult>> ApplyFixesToFileAsync(
        string filePath,
        List<CodeFix> fixes,
        CancellationToken cancellationToken)
    {
        var results = new List<FixResult>();

        if (!File.Exists(filePath))
        {
            foreach (var fix in fixes)
            {
                results.Add(FixResult.Failed(fix, filePath, "File not found"));
            }
            return results;
        }

        var originalContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        var lines = originalContent.Split('\n');
        var modifiedLines = new List<string>(lines);

        // Create backup if enabled and not dry run
        if (_createBackups && !_dryRun)
        {
            var backupPath = filePath + ".bak";
            await File.WriteAllTextAsync(backupPath, originalContent, cancellationToken);
        }

        // Apply fixes in reverse order (bottom to top) to preserve line numbers
        foreach (var fix in fixes.OrderByDescending(f => f.StartLine).ThenByDescending(f => f.StartColumn))
        {
            try
            {
                var result = ApplyFix(modifiedLines, fix);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(FixResult.Failed(fix, filePath, ex.Message));
            }
        }

        // Write modified content if not dry run and there were successful fixes
        if (!_dryRun && results.Any(r => r.Success))
        {
            var newContent = string.Join('\n', modifiedLines);
            await File.WriteAllTextAsync(filePath, newContent, cancellationToken);
        }

        return results;
    }

    private FixResult ApplyFix(List<string> lines, CodeFix fix)
    {
        try
        {
            switch (fix.Operation)
            {
                case FixOperation.Replace:
                    return ApplyReplaceFix(lines, fix);

                case FixOperation.Insert:
                    return ApplyInsertFix(lines, fix);

                case FixOperation.Delete:
                    return ApplyDeleteFix(lines, fix);

                case FixOperation.AddUsing:
                    return ApplyAddUsingFix(lines, fix);

                case FixOperation.RemoveUsing:
                    return ApplyRemoveUsingFix(lines, fix);

                default:
                    return FixResult.Failed(fix, fix.FilePath, $"Unknown fix operation: {fix.Operation}");
            }
        }
        catch (Exception ex)
        {
            return FixResult.Failed(fix, fix.FilePath, ex.Message);
        }
    }

    private FixResult ApplyReplaceFix(List<string> lines, CodeFix fix)
    {
        var lineIndex = fix.StartLine - 1;
        if (lineIndex < 0 || lineIndex >= lines.Count)
        {
            return FixResult.Failed(fix, fix.FilePath, $"Line {fix.StartLine} out of range");
        }

        var line = lines[lineIndex];

        // Single line replacement
        if (fix.StartLine == fix.EndLine)
        {
            var startCol = Math.Max(0, fix.StartColumn - 1);
            var endCol = Math.Min(line.Length, fix.EndColumn - 1);

            if (startCol <= line.Length)
            {
                var before = startCol > 0 ? line.Substring(0, startCol) : "";
                var after = endCol < line.Length ? line.Substring(endCol) : "";
                lines[lineIndex] = before + fix.ReplacementText + after;
                return FixResult.Succeeded(fix, fix.FilePath);
            }
        }
        else
        {
            // Multi-line replacement
            var startLineIndex = fix.StartLine - 1;
            var endLineIndex = fix.EndLine - 1;

            if (endLineIndex >= lines.Count)
            {
                return FixResult.Failed(fix, fix.FilePath, $"End line {fix.EndLine} out of range");
            }

            var startLine = lines[startLineIndex];
            var endLine = lines[endLineIndex];

            var before = fix.StartColumn > 1 ? startLine.Substring(0, fix.StartColumn - 1) : "";
            var after = fix.EndColumn <= endLine.Length ? endLine.Substring(fix.EndColumn - 1) : "";

            // Remove lines in between
            for (int i = endLineIndex; i > startLineIndex; i--)
            {
                lines.RemoveAt(i);
            }

            lines[startLineIndex] = before + fix.ReplacementText + after;
            return FixResult.Succeeded(fix, fix.FilePath);
        }

        return FixResult.Failed(fix, fix.FilePath, "Could not apply replacement");
    }

    private FixResult ApplyInsertFix(List<string> lines, CodeFix fix)
    {
        var lineIndex = fix.StartLine - 1;
        if (lineIndex < 0 || lineIndex > lines.Count)
        {
            return FixResult.Failed(fix, fix.FilePath, $"Line {fix.StartLine} out of range");
        }

        if (lineIndex == lines.Count)
        {
            lines.Add(fix.ReplacementText);
        }
        else
        {
            var line = lines[lineIndex];
            var col = Math.Max(0, fix.StartColumn - 1);

            if (col >= line.Length)
            {
                lines[lineIndex] = line + fix.ReplacementText;
            }
            else
            {
                lines[lineIndex] = line.Substring(0, col) + fix.ReplacementText + line.Substring(col);
            }
        }

        return FixResult.Succeeded(fix, fix.FilePath);
    }

    private FixResult ApplyDeleteFix(List<string> lines, CodeFix fix)
    {
        var lineIndex = fix.StartLine - 1;
        if (lineIndex < 0 || lineIndex >= lines.Count)
        {
            return FixResult.Failed(fix, fix.FilePath, $"Line {fix.StartLine} out of range");
        }

        if (fix.StartLine == fix.EndLine)
        {
            var line = lines[lineIndex];
            var startCol = Math.Max(0, fix.StartColumn - 1);
            var endCol = Math.Min(line.Length, fix.EndColumn - 1);

            lines[lineIndex] = line.Substring(0, startCol) + line.Substring(endCol);
        }
        else
        {
            // Multi-line delete
            var endLineIndex = Math.Min(fix.EndLine - 1, lines.Count - 1);
            for (int i = endLineIndex; i >= lineIndex; i--)
            {
                lines.RemoveAt(i);
            }
        }

        return FixResult.Succeeded(fix, fix.FilePath);
    }

    private FixResult ApplyAddUsingFix(List<string> lines, CodeFix fix)
    {
        // Find the best place to insert the using directive
        var insertIndex = 0;
        var lastUsingIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("using ") && !trimmed.StartsWith("using ("))
            {
                lastUsingIndex = i;
            }
            else if (trimmed.StartsWith("namespace ") || trimmed.StartsWith("public ") ||
                     trimmed.StartsWith("internal ") || trimmed.StartsWith("class ") ||
                     trimmed.StartsWith("["))
            {
                break;
            }
        }

        insertIndex = lastUsingIndex >= 0 ? lastUsingIndex + 1 : 0;
        lines.Insert(insertIndex, fix.ReplacementText);

        return FixResult.Succeeded(fix, fix.FilePath);
    }

    private FixResult ApplyRemoveUsingFix(List<string> lines, CodeFix fix)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith($"using {fix.OriginalText}"))
            {
                lines.RemoveAt(i);
                return FixResult.Succeeded(fix, fix.FilePath);
            }
        }

        return FixResult.Failed(fix, fix.FilePath, $"Using directive not found: {fix.OriginalText}");
    }
}
