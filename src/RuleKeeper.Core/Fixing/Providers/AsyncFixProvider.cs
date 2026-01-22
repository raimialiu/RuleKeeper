using System.Text.RegularExpressions;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Core.Fixing.Providers;

/// <summary>
/// Provides fixes for async programming violations.
/// </summary>
[FixProvider("Async Fix Provider", Category = "Async", Description = "Fixes async/await anti-patterns (.Result/.Wait() to await, ConfigureAwait)")]
public class AsyncFixProvider : IFixProvider
{
    public IEnumerable<string> SupportedRuleIds => new[]
    {
        "CS-ASYNC-002", // Blocking calls (.Result, .Wait())
        "CS-ASYNC-003", // Missing ConfigureAwait
    };

    public bool CanFix(Violation violation)
    {
        return SupportedRuleIds.Contains(violation.RuleId) &&
               !string.IsNullOrEmpty(violation.FilePath);
    }

    public IEnumerable<CodeFix> GetFixes(Violation violation, string sourceCode)
    {
        var fixes = violation.RuleId switch
        {
            "CS-ASYNC-002" => GetBlockingCallFixes(violation, sourceCode),
            "CS-ASYNC-003" => GetConfigureAwaitFixes(violation, sourceCode),
            _ => Enumerable.Empty<CodeFix>()
        };

        return fixes;
    }

    private IEnumerable<CodeFix> GetBlockingCallFixes(Violation violation, string sourceCode)
    {
        var lines = sourceCode.Split('\n');
        if (violation.StartLine <= 0 || violation.StartLine > lines.Length)
            yield break;

        var line = lines[violation.StartLine - 1];

        // Fix .Result -> await
        if (line.Contains(".Result"))
        {
            // Find the expression with .Result
            var match = Regex.Match(line, @"(\w+(?:\.\w+)*?)\.Result");
            if (match.Success)
            {
                var expr = match.Groups[1].Value;
                var startCol = line.IndexOf(match.Value) + 1;
                var endCol = startCol + match.Value.Length;

                yield return new CodeFix
                {
                    FixId = $"FIX-{violation.RuleId}-await-{Guid.NewGuid():N}",
                    RuleId = violation.RuleId,
                    Description = $"Replace '.Result' with 'await': {expr}.Result → await {expr}",
                    FilePath = violation.FilePath,
                    Operation = FixOperation.Replace,
                    StartLine = violation.StartLine,
                    StartColumn = startCol,
                    EndLine = violation.StartLine,
                    EndColumn = endCol,
                    OriginalText = match.Value,
                    ReplacementText = $"await {expr}",
                    Severity = violation.Severity,
                    IsSafe = false, // Requires method to be async
                    Category = "async"
                };
            }
        }

        // Fix .Wait() -> await
        if (line.Contains(".Wait()"))
        {
            var match = Regex.Match(line, @"(\w+(?:\.\w+)*?)\.Wait\(\)");
            if (match.Success)
            {
                var expr = match.Groups[1].Value;
                var startCol = line.IndexOf(match.Value) + 1;
                var endCol = startCol + match.Value.Length;

                yield return new CodeFix
                {
                    FixId = $"FIX-{violation.RuleId}-await-{Guid.NewGuid():N}",
                    RuleId = violation.RuleId,
                    Description = $"Replace '.Wait()' with 'await': {expr}.Wait() → await {expr}",
                    FilePath = violation.FilePath,
                    Operation = FixOperation.Replace,
                    StartLine = violation.StartLine,
                    StartColumn = startCol,
                    EndLine = violation.StartLine,
                    EndColumn = endCol,
                    OriginalText = match.Value,
                    ReplacementText = $"await {expr}",
                    Severity = violation.Severity,
                    IsSafe = false, // Requires method to be async
                    Category = "async"
                };
            }
        }

        // Fix .GetAwaiter().GetResult() -> await
        if (line.Contains(".GetAwaiter().GetResult()"))
        {
            var match = Regex.Match(line, @"(\w+(?:\.\w+)*?)\.GetAwaiter\(\)\.GetResult\(\)");
            if (match.Success)
            {
                var expr = match.Groups[1].Value;
                var startCol = line.IndexOf(match.Value) + 1;
                var endCol = startCol + match.Value.Length;

                yield return new CodeFix
                {
                    FixId = $"FIX-{violation.RuleId}-await-{Guid.NewGuid():N}",
                    RuleId = violation.RuleId,
                    Description = $"Replace '.GetAwaiter().GetResult()' with 'await'",
                    FilePath = violation.FilePath,
                    Operation = FixOperation.Replace,
                    StartLine = violation.StartLine,
                    StartColumn = startCol,
                    EndLine = violation.StartLine,
                    EndColumn = endCol,
                    OriginalText = match.Value,
                    ReplacementText = $"await {expr}",
                    Severity = violation.Severity,
                    IsSafe = false,
                    Category = "async"
                };
            }
        }
    }

    private IEnumerable<CodeFix> GetConfigureAwaitFixes(Violation violation, string sourceCode)
    {
        var lines = sourceCode.Split('\n');
        if (violation.StartLine <= 0 || violation.StartLine > lines.Length)
            yield break;

        var line = lines[violation.StartLine - 1];

        // Find await expressions without ConfigureAwait
        var match = Regex.Match(line, @"await\s+(\w+(?:\.\w+)*(?:\([^)]*\))?)(?!\s*\.ConfigureAwait)");
        if (match.Success)
        {
            var expr = match.Groups[1].Value;
            var fullMatch = match.Value;
            var startCol = line.IndexOf(fullMatch) + 1;
            var endCol = startCol + fullMatch.Length;

            yield return new CodeFix
            {
                FixId = $"FIX-{violation.RuleId}-{Guid.NewGuid():N}",
                RuleId = violation.RuleId,
                Description = $"Add ConfigureAwait(false)",
                FilePath = violation.FilePath,
                Operation = FixOperation.Replace,
                StartLine = violation.StartLine,
                StartColumn = startCol,
                EndLine = violation.StartLine,
                EndColumn = endCol,
                OriginalText = fullMatch,
                ReplacementText = $"await {expr}.ConfigureAwait(false)",
                Severity = violation.Severity,
                IsSafe = true,
                Category = "async"
            };
        }
    }
}
