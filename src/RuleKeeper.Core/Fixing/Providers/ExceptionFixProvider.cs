using System.Text.RegularExpressions;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Core.Fixing.Providers;

/// <summary>
/// Provides fixes for exception handling violations.
/// </summary>
[FixProvider("Exception Fix Provider", Category = "Exceptions", Description = "Fixes exception handling issues (empty catch, throw ex → throw)")]
public class ExceptionFixProvider : IFixProvider
{
    public IEnumerable<string> SupportedRuleIds => new[]
    {
        "CS-EXC-001", // Empty catch blocks
        "CS-EXC-004", // Throw vs throw ex
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
            "CS-EXC-001" => GetEmptyCatchFixes(violation, sourceCode),
            "CS-EXC-004" => GetThrowExFixes(violation, sourceCode),
            _ => Enumerable.Empty<CodeFix>()
        };

        return fixes;
    }

    private IEnumerable<CodeFix> GetEmptyCatchFixes(Violation violation, string sourceCode)
    {
        var lines = sourceCode.Split('\n');
        if (violation.StartLine <= 0 || violation.StartLine > lines.Length)
            yield break;

        // Find the catch block and add a comment or logging placeholder
        var line = lines[violation.StartLine - 1];
        var indent = new string(' ', line.TakeWhile(char.IsWhiteSpace).Count());

        // Find the opening brace of the catch block
        var braceIndex = -1;
        for (int i = violation.StartLine - 1; i < lines.Length && i < violation.StartLine + 3; i++)
        {
            if (lines[i].Contains("{"))
            {
                braceIndex = i;
                break;
            }
        }

        if (braceIndex >= 0)
        {
            var insertLine = braceIndex + 1; // Insert after the opening brace
            var insertIndent = indent + "    ";

            yield return new CodeFix
            {
                FixId = $"FIX-{violation.RuleId}-comment-{Guid.NewGuid():N}",
                RuleId = violation.RuleId,
                Description = "Add TODO comment for empty catch block",
                FilePath = violation.FilePath,
                Operation = FixOperation.Insert,
                StartLine = insertLine + 1,
                StartColumn = 1,
                EndLine = insertLine + 1,
                EndColumn = 1,
                OriginalText = "",
                ReplacementText = $"{insertIndent}// TODO: Handle or log exception appropriately\n",
                Severity = violation.Severity,
                IsSafe = true,
                Category = "exceptions"
            };

            yield return new CodeFix
            {
                FixId = $"FIX-{violation.RuleId}-log-{Guid.NewGuid():N}",
                RuleId = violation.RuleId,
                Description = "Add logging placeholder for empty catch block",
                FilePath = violation.FilePath,
                Operation = FixOperation.Insert,
                StartLine = insertLine + 1,
                StartColumn = 1,
                EndLine = insertLine + 1,
                EndColumn = 1,
                OriginalText = "",
                ReplacementText = $"{insertIndent}// Log.Error(ex, \"An error occurred\");\n",
                Severity = violation.Severity,
                IsSafe = true,
                Category = "exceptions"
            };
        }
    }

    private IEnumerable<CodeFix> GetThrowExFixes(Violation violation, string sourceCode)
    {
        var lines = sourceCode.Split('\n');
        if (violation.StartLine <= 0 || violation.StartLine > lines.Length)
            yield break;

        var line = lines[violation.StartLine - 1];

        // Match "throw ex;" pattern
        var match = Regex.Match(line, @"throw\s+(\w+)\s*;");
        if (match.Success)
        {
            var exName = match.Groups[1].Value;
            var startCol = line.IndexOf(match.Value) + 1;
            var endCol = startCol + match.Value.Length;

            yield return new CodeFix
            {
                FixId = $"FIX-{violation.RuleId}-{Guid.NewGuid():N}",
                RuleId = violation.RuleId,
                Description = $"Replace 'throw {exName};' with 'throw;' to preserve stack trace",
                FilePath = violation.FilePath,
                Operation = FixOperation.Replace,
                StartLine = violation.StartLine,
                StartColumn = startCol,
                EndLine = violation.StartLine,
                EndColumn = endCol,
                OriginalText = match.Value,
                ReplacementText = "throw;",
                Severity = violation.Severity,
                IsSafe = true,
                Category = "exceptions"
            };
        }
    }
}
