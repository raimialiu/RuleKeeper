using System.Text.RegularExpressions;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Enhanced regex validator with capture groups and message interpolation.
/// </summary>
public class RegexValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _name;
    private readonly string? _description;
    private readonly PatternConfig _pattern;
    private readonly string? _defaultMessage;
    private Regex? _compiledRegex;

    public override string ValidatorId => _validatorId;
    public override string Name => _name;
    public override string? Description => _description;

    public RegexValidator(string validatorId, string name, string? description, PatternConfig pattern, string? defaultMessage = null)
    {
        _validatorId = validatorId;
        _name = name;
        _description = description;
        _pattern = pattern;
        _defaultMessage = defaultMessage;
    }

    public override void Initialize(Dictionary<string, object> parameters)
    {
        base.Initialize(parameters);

        // Compile the regex if not already done
        if (_pattern.Regex != null && _compiledRegex == null)
        {
            var options = _pattern.GetRegexOptions();
            _compiledRegex = new Regex(_pattern.Regex, options);
        }
    }

    public override Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();

        if (_compiledRegex == null && _pattern.Regex != null)
        {
            _compiledRegex = new Regex(_pattern.Regex, _pattern.GetRegexOptions());
        }

        if (_compiledRegex == null)
            return Task.FromResult<IEnumerable<Violation>>(violations);

        var sourceText = context.SourceText;
        var matches = _compiledRegex.Matches(sourceText);

        // Check match count constraints
        var matchCount = matches.Count;
        var shouldViolate = false;

        if (_pattern.Negate)
        {
            // Anti-pattern: violations when matches ARE found
            shouldViolate = matchCount > 0;
        }
        else
        {
            // Pattern: check min/max matches
            if (_pattern.MinMatches > 0 && matchCount < _pattern.MinMatches)
            {
                // Not enough matches - single violation
                violations.Add(CreateViolation(
                    context,
                    FormatMessage(context, null, $"Expected at least {_pattern.MinMatches} matches, found {matchCount}"),
                    1, 1));
                return Task.FromResult<IEnumerable<Violation>>(violations);
            }

            if (_pattern.MaxMatches >= 0 && matchCount > _pattern.MaxMatches)
            {
                shouldViolate = true;
            }
        }

        if (!shouldViolate && !_pattern.Negate)
            return Task.FromResult<IEnumerable<Violation>>(violations);

        // Report violations for each match
        foreach (Match match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (startLine, startColumn, endLine, endColumn) = GetLineAndColumn(sourceText, match.Index, match.Length);
            var codeSnippet = GetCodeSnippet(context, startLine);

            violations.Add(CreateViolation(
                context,
                FormatMessage(context, match, null),
                startLine,
                startColumn,
                endLine,
                endColumn,
                codeSnippet));
        }

        return Task.FromResult<IEnumerable<Violation>>(violations);
    }

    private string FormatMessage(ValidationContext context, Match? match, string? defaultMsg)
    {
        var message = context.CustomMessage
            ?? _pattern.MessageTemplate
            ?? _defaultMessage
            ?? defaultMsg
            ?? (_pattern.Negate
                ? $"Pattern '{_pattern.Regex}' should not match"
                : $"Pattern '{_pattern.Regex}' constraint violated");

        // Interpolate capture groups
        if (match != null && _pattern.Captures != null)
        {
            foreach (var (groupName, varName) in _pattern.Captures)
            {
                var group = match.Groups[groupName];
                if (group.Success)
                {
                    message = message.Replace($"{{{varName}}}", group.Value);
                }
            }
        }

        // Also support direct group names
        if (match != null)
        {
            foreach (Group group in match.Groups)
            {
                if (!string.IsNullOrEmpty(group.Name) && group.Name != "0")
                {
                    message = message.Replace($"{{{group.Name}}}", group.Value);
                }
            }

            // Replace {match} with the full match
            message = message.Replace("{match}", match.Value);
        }

        return message;
    }

    private static (int startLine, int startColumn, int endLine, int endColumn) GetLineAndColumn(string text, int index, int length)
    {
        var startLine = 1;
        var startColumn = 1;
        var endLine = 1;
        var endColumn = 1;
        var currentColumn = 1;

        for (var i = 0; i < text.Length && i <= index + length; i++)
        {
            if (i == index)
            {
                startLine = endLine;
                startColumn = currentColumn;
            }

            if (i == index + length)
            {
                endColumn = currentColumn;
                break;
            }

            if (text[i] == '\n')
            {
                endLine++;
                currentColumn = 1;
            }
            else
            {
                currentColumn++;
            }
        }

        return (startLine, startColumn, endLine, endColumn);
    }

    private static string? GetCodeSnippet(ValidationContext context, int lineNumber)
    {
        return context.GetLine(lineNumber)?.TrimEnd('\r', '\n');
    }
}

/// <summary>
/// Simple regex validator for backward compatibility.
/// </summary>
public class SimpleRegexValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _name;
    private readonly string _pattern;
    private readonly bool _isAntiPattern;
    private readonly string? _defaultMessage;
    private readonly RegexOptions _options;
    private Regex? _compiledRegex;

    public override string ValidatorId => _validatorId;
    public override string Name => _name;

    public SimpleRegexValidator(
        string validatorId,
        string name,
        string pattern,
        bool isAntiPattern,
        string? defaultMessage = null,
        RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnoreCase)
    {
        _validatorId = validatorId;
        _name = name;
        _pattern = pattern;
        _isAntiPattern = isAntiPattern;
        _defaultMessage = defaultMessage;
        _options = options;
    }

    public override Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();

        _compiledRegex ??= new Regex(_pattern, _options);

        var sourceText = context.SourceText;
        var matches = _compiledRegex.Matches(sourceText);

        if (!_isAntiPattern && matches.Count == 0)
        {
            // Pattern should match but doesn't - this is context-dependent
            return Task.FromResult<IEnumerable<Violation>>(violations);
        }

        if (_isAntiPattern && matches.Count == 0)
        {
            // Anti-pattern found no matches - good!
            return Task.FromResult<IEnumerable<Violation>>(violations);
        }

        foreach (Match match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (startLine, startColumn, endLine, endColumn) = GetLineAndColumn(sourceText, match.Index, match.Length);
            var codeSnippet = context.GetLine(startLine)?.TrimEnd('\r', '\n');

            var message = context.CustomMessage
                ?? _defaultMessage
                ?? (_isAntiPattern
                    ? $"Anti-pattern detected: {match.Value}"
                    : $"Pattern '{_pattern}' matched: {match.Value}");

            violations.Add(CreateViolation(
                context,
                message,
                startLine,
                startColumn,
                endLine,
                endColumn,
                codeSnippet));
        }

        return Task.FromResult<IEnumerable<Violation>>(violations);
    }

    private static (int startLine, int startColumn, int endLine, int endColumn) GetLineAndColumn(string text, int index, int length)
    {
        var startLine = 1;
        var startColumn = 1;
        var endLine = 1;
        var endColumn = 1;
        var currentColumn = 1;

        for (var i = 0; i < text.Length && i <= index + length; i++)
        {
            if (i == index)
            {
                startLine = endLine;
                startColumn = currentColumn;
            }

            if (i == index + length)
            {
                endColumn = currentColumn;
                break;
            }

            if (text[i] == '\n')
            {
                endLine++;
                currentColumn = 1;
            }
            else
            {
                currentColumn++;
            }
        }

        return (startLine, startColumn, endLine, endColumn);
    }
}
