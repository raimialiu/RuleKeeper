using System.Text.RegularExpressions;

namespace RuleKeeper.Sdk;

/// <summary>
/// Base class for implementing fix providers with common functionality.
/// </summary>
public abstract class BaseFixProvider : IFixProvider
{
    /// <inheritdoc />
    public abstract IEnumerable<string> SupportedRuleIds { get; }

    /// <inheritdoc />
    public virtual bool CanFix(Violation violation)
    {
        return SupportedRuleIds.Contains(violation.RuleId) &&
               !string.IsNullOrEmpty(violation.FilePath);
    }

    /// <inheritdoc />
    public abstract IEnumerable<CodeFix> GetFixes(Violation violation, string sourceCode);

    /// <summary>
    /// Gets the lines of the source code as an array.
    /// </summary>
    protected static string[] GetLines(string sourceCode) => sourceCode.Split('\n');

    /// <summary>
    /// Gets a specific line from the source code (1-based line number).
    /// </summary>
    protected static string? GetLine(string sourceCode, int lineNumber)
    {
        var lines = GetLines(sourceCode);
        if (lineNumber <= 0 || lineNumber > lines.Length)
            return null;
        return lines[lineNumber - 1];
    }

    /// <summary>
    /// Gets the indentation of a line.
    /// </summary>
    protected static string GetIndentation(string line)
    {
        return new string(' ', line.TakeWhile(char.IsWhiteSpace).Count());
    }

    /// <summary>
    /// Finds a pattern in a line and returns the match.
    /// </summary>
    protected static Match? FindPattern(string line, string pattern)
    {
        var match = Regex.Match(line, pattern);
        return match.Success ? match : null;
    }

    /// <summary>
    /// Creates a replacement fix for renaming an identifier.
    /// </summary>
    protected static CodeFix CreateRenameFix(
        Violation violation,
        string line,
        string oldName,
        string newName,
        string description)
    {
        var startCol = line.IndexOf(oldName) + 1;
        var endCol = startCol + oldName.Length;

        return new CodeFix
        {
            FixId = $"FIX-{violation.RuleId}-{Guid.NewGuid():N}",
            RuleId = violation.RuleId,
            Description = description,
            FilePath = violation.FilePath,
            Operation = FixOperation.Replace,
            StartLine = violation.StartLine,
            StartColumn = startCol,
            EndLine = violation.StartLine,
            EndColumn = endCol,
            OriginalText = oldName,
            ReplacementText = newName,
            Severity = violation.Severity,
            IsSafe = true,
            Category = ExtractCategory(violation.RuleId)
        };
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    protected static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove leading underscores
        input = input.TrimStart('_');

        if (string.IsNullOrEmpty(input))
            return input;

        // Already PascalCase?
        if (char.IsUpper(input[0]))
            return input;

        // Convert first char to upper
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    protected static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove leading underscores
        input = input.TrimStart('_');

        if (string.IsNullOrEmpty(input))
            return input;

        // Already camelCase?
        if (char.IsLower(input[0]))
            return input;

        // Convert first char to lower
        return char.ToLower(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// Converts a string to _camelCase (for private fields).
    /// </summary>
    protected static string ToUnderscoreCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Already has underscore prefix?
        if (input.StartsWith("_"))
            return input;

        // Add underscore and make camelCase
        var camel = ToCamelCase(input);
        return "_" + camel;
    }

    private static string ExtractCategory(string ruleId)
    {
        var parts = ruleId.Split('-');
        if (parts.Length >= 2)
        {
            return parts[1].ToLowerInvariant() switch
            {
                "name" => "naming",
                "sec" => "security",
                "async" => "async",
                "exc" => "exceptions",
                "design" => "design",
                "di" => "dependency-injection",
                _ => parts[1].ToLowerInvariant()
            };
        }
        return "general";
    }
}
