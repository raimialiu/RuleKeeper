using Microsoft.CodeAnalysis;
using RuleKeeper.Sdk.Abstractions;

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
    /// The programming language of the source file where the violation occurred.
    /// </summary>
    public Language Language { get; init; } = Language.CSharp;

    /// <summary>
    /// Creates a Violation from a SourceLocation.
    /// </summary>
    /// <param name="location">The source location.</param>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="language">The programming language.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <param name="codeSnippet">Optional code snippet.</param>
    /// <returns>A new Violation instance.</returns>
    public static Violation FromSourceLocation(
        SourceLocation location,
        string ruleId,
        string ruleName,
        string message,
        SeverityLevel severity,
        Language language = Language.CSharp,
        string? fixHint = null,
        string? codeSnippet = null)
    {
        return new Violation
        {
            RuleId = ruleId,
            RuleName = ruleName,
            Message = message,
            Severity = severity,
            Language = language,
            FilePath = location.FilePath,
            StartLine = location.StartLine,
            StartColumn = location.StartColumn,
            EndLine = location.EndLine,
            EndColumn = location.EndColumn,
            FixHint = fixHint,
            CodeSnippet = codeSnippet
        };
    }

    /// <summary>
    /// Creates a Violation from a unified syntax node.
    /// </summary>
    /// <param name="node">The syntax node where the violation occurred.</param>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <param name="sourceText">Optional source text for snippet extraction.</param>
    /// <returns>A new Violation instance.</returns>
    public static Violation FromUnifiedNode(
        IUnifiedSyntaxNode node,
        string ruleId,
        string ruleName,
        string message,
        SeverityLevel severity,
        string? fixHint = null,
        string? sourceText = null)
    {
        string? codeSnippet = null;
        if (sourceText != null && node.Location.IsValid)
        {
            var lines = sourceText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var lineIndex = node.Location.StartLine - 1;
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                codeSnippet = lines[lineIndex];
            }
        }

        return new Violation
        {
            RuleId = ruleId,
            RuleName = ruleName,
            Message = message,
            Severity = severity,
            Language = node.Language,
            FilePath = node.Location.FilePath,
            StartLine = node.Location.StartLine,
            StartColumn = node.Location.StartColumn,
            EndLine = node.Location.EndLine,
            EndColumn = node.Location.EndColumn,
            FixHint = fixHint,
            CodeSnippet = codeSnippet
        };
    }

    /// <summary>
    /// Creates a Violation from a Roslyn Location.
    /// </summary>
    /// <param name="location">The Roslyn location.</param>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new Violation instance.</returns>
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
            var lineIndex = lineSpan.StartLinePosition.Line;
            if (lineIndex >= 0 && lineIndex < text.Lines.Count)
            {
                var line = text.Lines[lineIndex];
                codeSnippet = line.ToString();
            }
        }

        return new Violation
        {
            RuleId = ruleId,
            RuleName = ruleName,
            Message = message,
            Severity = severity,
            Language = Language.CSharp,
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
