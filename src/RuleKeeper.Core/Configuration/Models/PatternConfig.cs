using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Enhanced regex pattern configuration with captures, options, and scope.
/// </summary>
public class PatternConfig
{
    /// <summary>
    /// The regex pattern to match.
    /// </summary>
    [YamlMember(Alias = "regex")]
    public string? Regex { get; set; }

    /// <summary>
    /// Named capture groups to extract from matches.
    /// Maps capture group names to variable names for use in messages.
    /// </summary>
    [YamlMember(Alias = "captures")]
    public Dictionary<string, string>? Captures { get; set; }

    /// <summary>
    /// Regex options: ignorecase, multiline, singleline, compiled.
    /// </summary>
    [YamlMember(Alias = "options")]
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// Scope to limit pattern matching: file, class, method, block.
    /// </summary>
    [YamlMember(Alias = "scope")]
    public string Scope { get; set; } = "file";

    /// <summary>
    /// Whether this pattern should NOT match (anti-pattern).
    /// </summary>
    [YamlMember(Alias = "negate")]
    public bool Negate { get; set; } = false;

    /// <summary>
    /// Minimum number of matches required (0 = any).
    /// </summary>
    [YamlMember(Alias = "min_matches")]
    public int MinMatches { get; set; } = 0;

    /// <summary>
    /// Maximum number of matches allowed (-1 = unlimited).
    /// </summary>
    [YamlMember(Alias = "max_matches")]
    public int MaxMatches { get; set; } = -1;

    /// <summary>
    /// Message template with capture group interpolation.
    /// Use {capture_name} to reference captured values.
    /// </summary>
    [YamlMember(Alias = "message_template")]
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Gets the compiled RegexOptions from the Options list.
    /// </summary>
    public System.Text.RegularExpressions.RegexOptions GetRegexOptions()
    {
        var options = System.Text.RegularExpressions.RegexOptions.None;

        foreach (var opt in Options)
        {
            switch (opt.ToLowerInvariant())
            {
                case "ignorecase":
                    options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    break;
                case "multiline":
                    options |= System.Text.RegularExpressions.RegexOptions.Multiline;
                    break;
                case "singleline":
                    options |= System.Text.RegularExpressions.RegexOptions.Singleline;
                    break;
                case "compiled":
                    options |= System.Text.RegularExpressions.RegexOptions.Compiled;
                    break;
                case "ignorepatternwhitespace":
                    options |= System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace;
                    break;
            }
        }

        return options;
    }
}
