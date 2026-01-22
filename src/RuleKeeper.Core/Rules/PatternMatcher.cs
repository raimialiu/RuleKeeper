using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Rules;

/// <summary>
/// Utility class for matching patterns in source code.
/// </summary>
public class PatternMatcher
{
    /// <summary>
    /// Finds all matches of an anti-pattern in the source text.
    /// </summary>
    public IEnumerable<PatternMatch> FindAntiPatternMatches(
        string sourceText,
        string antiPattern,
        RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnoreCase)
    {
        var regex = new Regex(antiPattern, options);
        foreach (Match match in regex.Matches(sourceText))
        {
            yield return new PatternMatch
            {
                Value = match.Value,
                Index = match.Index,
                Length = match.Length,
                Groups = match.Groups.Cast<Group>()
                    .Select(g => new PatternMatchGroup { Value = g.Value, Index = g.Index })
                    .ToList()
            };
        }
    }

    /// <summary>
    /// Validates that a value matches a required pattern.
    /// </summary>
    public bool ValidatePattern(string value, string pattern)
    {
        var regex = new Regex(pattern);
        return regex.IsMatch(value);
    }

    /// <summary>
    /// Creates violations from pattern matches.
    /// </summary>
    public IEnumerable<Violation> CreateViolationsFromMatches(
        IEnumerable<PatternMatch> matches,
        SourceText sourceText,
        string filePath,
        string ruleId,
        string ruleName,
        string message,
        SeverityLevel severity,
        string? fixHint = null)
    {
        foreach (var match in matches)
        {
            var lineSpan = sourceText.Lines.GetLinePositionSpan(
                TextSpan.FromBounds(match.Index, match.Index + match.Length));

            yield return new Violation
            {
                RuleId = ruleId,
                RuleName = ruleName,
                Message = message,
                Severity = severity,
                FilePath = filePath,
                StartLine = lineSpan.Start.Line + 1,
                StartColumn = lineSpan.Start.Character + 1,
                EndLine = lineSpan.End.Line + 1,
                EndColumn = lineSpan.End.Character + 1,
                FixHint = fixHint,
                CodeSnippet = sourceText.Lines[lineSpan.Start.Line].ToString()
            };
        }
    }

    /// <summary>
    /// Common patterns for C# naming conventions.
    /// </summary>
    public static class CommonPatterns
    {
        public const string PascalCase = @"^[A-Z][a-zA-Z0-9]*$";
        public const string CamelCase = @"^[a-z][a-zA-Z0-9]*$";
        public const string InterfacePrefix = @"^I[A-Z][a-zA-Z0-9]*$";
        public const string AsyncSuffix = @"^.+Async$";
        public const string PrivateFieldPrefix = @"^_[a-z][a-zA-Z0-9]*$";
        public const string ConstantCase = @"^[A-Z][A-Z0-9_]*$";
        public const string TypeParameterPrefix = @"^T([A-Z][a-zA-Z0-9]*)?$";
    }

    /// <summary>
    /// Common anti-patterns to detect.
    /// </summary>
    public static class CommonAntiPatterns
    {
        public const string HardcodedPassword = @"(password|pwd|passwd)\s*=\s*[""'][^""']+[""']";
        public const string HardcodedSecret = @"(secret|apikey|api_key|token)\s*=\s*[""'][^""']+[""']";
        public const string HardcodedConnectionString = @"(connectionstring|connstr)\s*=\s*[""'][^""']+[""']";
        public const string TaskResult = @"\.Result\b";
        public const string TaskWait = @"\.Wait\s*\(";
        public const string EmptyCatch = @"catch\s*\([^)]*\)\s*\{\s*\}";
        public const string ConsoleWriteLine = @"Console\.(WriteLine|Write)\s*\(";
        public const string GotoStatement = @"\bgoto\s+\w+\s*;";
    }
}

/// <summary>
/// Represents a pattern match result.
/// </summary>
public class PatternMatch
{
    public required string Value { get; init; }
    public required int Index { get; init; }
    public required int Length { get; init; }
    public List<PatternMatchGroup> Groups { get; init; } = new();
}

/// <summary>
/// Represents a capture group in a pattern match.
/// </summary>
public class PatternMatchGroup
{
    public required string Value { get; init; }
    public required int Index { get; init; }
}
