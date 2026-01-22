using Microsoft.CodeAnalysis;

namespace RuleKeeper.Sdk;

/// <summary>
/// Represents a rule violation found during analysis.
/// </summary>
public class Violation
{
    /// <summary>
    /// The unique identifier of the rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// The name of the rule that was violated.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// A message describing the violation.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The severity level of the violation.
    /// </summary>
    public required SeverityLevel Severity { get; init; }

    /// <summary>
    /// The file path where the violation occurred.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The line number where the violation starts.
    /// </summary>
    public required int StartLine { get; init; }

    /// <summary>
    /// The column number where the violation starts.
    /// </summary>
    public required int StartColumn { get; init; }

    /// <summary>
    /// The line number where the violation ends.
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// The column number where the violation ends.
    /// </summary>
    public required int EndColumn { get; init; }

    /// <summary>
    /// Optional hint for how to fix the violation.
    /// </summary>
    public string? FixHint { get; init; }

    /// <summary>
    /// The code snippet containing the violation.
    /// </summary>
    public string? CodeSnippet { get; init; }

    /// <summary>
    /// Creates a Violation from a Roslyn Location.
    /// </summary>
    public static Violation FromLocation(
        Location location,
        string ruleId,
        string ruleName,
        string message,
        SeverityLevel severity,
        string? fixHint = null)
    {
        var lineSpan = location.GetLineSpan();
        var sourceTree = location.SourceTree;

        string? codeSnippet = null;
        if (sourceTree != null)
        {
            var text = sourceTree.GetText();
            var line = text.Lines[lineSpan.StartLinePosition.Line];
            codeSnippet = line.ToString();
        }

        return new Violation
        {
            RuleId = ruleId,
            RuleName = ruleName,
            Message = message,
            Severity = severity,
            FilePath = lineSpan.Path ?? location.SourceTree?.FilePath ?? "unknown",
            StartLine = lineSpan.StartLinePosition.Line + 1,
            StartColumn = lineSpan.StartLinePosition.Character + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            EndColumn = lineSpan.EndLinePosition.Character + 1,
            FixHint = fixHint,
            CodeSnippet = codeSnippet
        };
    }
}
