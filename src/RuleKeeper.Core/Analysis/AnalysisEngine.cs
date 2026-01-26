using System.Collections.Concurrent;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Core.Rules;
using RuleKeeper.Sdk;
using AnalysisContext = RuleKeeper.Sdk.AnalysisContext;

namespace RuleKeeper.Core.Analysis;

/// <summary>
/// Main orchestrator for code analysis.
/// </summary>
public class AnalysisEngine : IDisposable
{
    private readonly RuleRegistry _ruleRegistry;
    private readonly RuleExecutor _ruleExecutor;
    private readonly RoslynWorkspaceLoader _workspaceLoader;
    private readonly ConfigurationValidator _configValidator;

    public AnalysisEngine(RuleRegistry? ruleRegistry = null)
    {
        _ruleRegistry = ruleRegistry ?? new RuleRegistry();
        _ruleExecutor = new RuleExecutor(_ruleRegistry);
        _workspaceLoader = new RoslynWorkspaceLoader();
        _configValidator = new ConfigurationValidator();
    }

    /// <summary>
    /// Gets the rule registry for this engine.
    /// </summary>
    public RuleRegistry RuleRegistry => _ruleRegistry;

    /// <summary>
    /// Analyzes code at the specified path using the given configuration.
    /// </summary>
    public async Task<AnalysisReport> AnalyzeAsync(
        string path,
        RuleKeeperConfig config,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Validate configuration
        _configValidator.ValidateAndThrow(config);

        var report = new AnalysisReport
        {
            AnalyzedPath = Path.GetFullPath(path)
        };

        try
        {
            progress?.Report(new AnalysisProgress { Stage = "Loading files", Current = 0, Total = 0 });
            var analysisUnits = await _workspaceLoader.LoadFilesAsync(path, config.Scan, cancellationToken);

            report.AnalyzedFiles.AddRange(analysisUnits.Select(u => u.FilePath));
            if (config.Scan.Parallel && analysisUnits.Count > 1)
            {
                await AnalyzeParallelAsync(analysisUnits, config, report, progress, cancellationToken);
            }
            else
            {
                await AnalyzeSequentialAsync(analysisUnits, config, report, progress, cancellationToken);
            }
            
            ApplyOutputLimits(report, config.Output);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            report.Errors.Add(new AnalysisError
            {
                FilePath = path,
                Message = $"Analysis failed: {ex.Message}",
                Exception = ex
            });
        }
        finally
        {
            report.EndTime = DateTime.UtcNow;
        }

        return report;
    }

    /// <summary>
    /// Analyzes a single file.
    /// </summary>
    public async Task<List<Violation>> AnalyzeFileAsync(
        string filePath,
        RuleKeeperConfig config,
        CancellationToken cancellationToken = default)
    {
        var units = await _workspaceLoader.LoadFilesAsync(filePath, config.Scan, cancellationToken);
        if (units.Count == 0)
            return new List<Violation>();

        var unit = units[0];
        var context = CreateContext(unit);
        return await _ruleExecutor.ExecuteAsync(context, config, cancellationToken);
    }

    private async Task AnalyzeSequentialAsync(
        List<FileAnalysisUnit> units,
        RuleKeeperConfig config,
        AnalysisReport report,
        IProgress<AnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < units.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var unit = units[i];
            progress?.Report(new AnalysisProgress
            {
                Stage = "Analyzing",
                Current = i + 1,
                Total = units.Count,
                CurrentFile = unit.FilePath
            });

            try
            {
                var context = CreateContext(unit);
                var violations = await _ruleExecutor.ExecuteAsync(context, config, cancellationToken);
                report.Violations.AddRange(violations);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                report.Errors.Add(new AnalysisError
                {
                    FilePath = unit.FilePath,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
    }

    private async Task AnalyzeParallelAsync(
        List<FileAnalysisUnit> units,
        RuleKeeperConfig config,
        AnalysisReport report,
        IProgress<AnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var violations = new ConcurrentBag<Violation>();
        var errors = new ConcurrentBag<AnalysisError>();
        var processedCount = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = config.Scan.MaxParallelism > 0
                ? config.Scan.MaxParallelism
                : Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(units, parallelOptions, async (unit, ct) =>
        {
            try
            {
                var context = CreateContext(unit);
                var fileViolations = await _ruleExecutor.ExecuteAsync(context, config, ct);

                foreach (var violation in fileViolations)
                {
                    violations.Add(violation);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add(new AnalysisError
                {
                    FilePath = unit.FilePath,
                    Message = ex.Message,
                    Exception = ex
                });
            }

            var current = Interlocked.Increment(ref processedCount);
            progress?.Report(new AnalysisProgress
            {
                Stage = "Analyzing",
                Current = current,
                Total = units.Count,
                CurrentFile = unit.FilePath
            });
        });

        report.Violations.AddRange(violations);
        report.Errors.AddRange(errors);
    }

    private AnalysisContext CreateContext(FileAnalysisUnit unit)
    {
        return new AnalysisContext
        {
            SyntaxTree = unit.SyntaxTree,
            SemanticModel = unit.SemanticModel,
            Compilation = unit.Compilation,
            FilePath = unit.FilePath
        };
    }

    private void ApplyOutputLimits(AnalysisReport report, OutputConfig output)
    {
        // Filter by minimum severity
        report.Violations.RemoveAll(v => v.Severity < output.MinSeverity);

        // Apply per-rule limit
        if (output.MaxPerRule.HasValue)
        {
            var byRule = report.Violations.GroupBy(v => v.RuleId).ToList();
            report.Violations.Clear();

            foreach (var group in byRule)
            {
                report.Violations.AddRange(group.Take(output.MaxPerRule.Value));
            }
        }
        
        if (output.MaxTotal.HasValue && report.Violations.Count > output.MaxTotal.Value)
        {
            report.Violations = report.Violations
                .OrderByDescending(v => v.Severity)
                .ThenBy(v => v.FilePath)
                .ThenBy(v => v.StartLine)
                .Take(output.MaxTotal.Value)
                .ToList();
        }
    }

    public void Dispose()
    {
        _workspaceLoader.Dispose();
    }
}