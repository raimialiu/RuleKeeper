using RuleKeeper.Sdk;
using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Defines configuration for a single rule.
/// Supports multiple validation modes: built-in analyzers, patterns, AST queries,
/// expressions, scripts, and external validators.
/// </summary>
public class RuleDefinition
{
    /// <summary>
    /// The unique identifier for the rule.
    /// </summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>
    /// Display name for the rule.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Description of what the rule checks.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Severity level for violations of this rule.
    /// </summary>
    [YamlMember(Alias = "severity")]
    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;

    /// <summary>
    /// Whether this rule is enabled.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to skip this rule (alias for enabled=false).
    /// </summary>
    [YamlMember(Alias = "skip")]
    public bool Skip { get; set; } = false;

    /// <summary>
    /// Simple regex pattern for valid code (must match).
    /// For more advanced pattern matching, use PatternMatch instead.
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// Simple regex pattern for invalid code (must not match).
    /// For more advanced anti-pattern matching, use AntiPatternMatch instead.
    /// </summary>
    [YamlMember(Alias = "anti_pattern")]
    public string? AntiPattern { get; set; }

    /// <summary>
    /// Enhanced pattern configuration with captures, options, and scope.
    /// </summary>
    [YamlMember(Alias = "pattern_match")]
    public PatternConfig? PatternMatch { get; set; }

    /// <summary>
    /// Enhanced anti-pattern configuration with captures, options, and scope.
    /// </summary>
    [YamlMember(Alias = "anti_pattern_match")]
    public PatternConfig? AntiPatternMatch { get; set; }

    /// <summary>
    /// AST query configuration for declarative node matching.
    /// </summary>
    [YamlMember(Alias = "ast_query")]
    public AstQueryConfig? AstQuery { get; set; }

    /// <summary>
    /// Multi-pattern match configuration for AND/OR/NONE logic.
    /// </summary>
    [YamlMember(Alias = "match")]
    public MatchConfig? Match { get; set; }

    /// <summary>
    /// C# expression-based validation.
    /// </summary>
    [YamlMember(Alias = "expression")]
    public ExpressionConfig? Expression { get; set; }

    /// <summary>
    /// Full C# script-based validation.
    /// </summary>
    [YamlMember(Alias = "script")]
    public ScriptConfig? Script { get; set; }

    /// <summary>
    /// Validator reference or inline definition.
    /// </summary>
    [YamlMember(Alias = "validator")]
    public ValidatorConfig? Validator { get; set; }

    /// <summary>
    /// List of syntax elements this rule applies to.
    /// </summary>
    [YamlMember(Alias = "applies_to")]
    public List<string> AppliesTo { get; set; } = new();

    /// <summary>
    /// File pattern to limit which files this rule applies to.
    /// </summary>
    [YamlMember(Alias = "file_pattern")]
    public string? FilePattern { get; set; }

    /// <summary>
    /// Name of a custom validator to use (reference to custom_validators section).
    /// </summary>
    [YamlMember(Alias = "custom_validator")]
    public string? CustomValidator { get; set; }

    /// <summary>
    /// Message to display when the rule is violated.
    /// Supports template interpolation with {capture_name} for pattern captures.
    /// </summary>
    [YamlMember(Alias = "message")]
    public string? Message { get; set; }

    /// <summary>
    /// Hint for how to fix violations of this rule.
    /// </summary>
    [YamlMember(Alias = "fix_hint")]
    public string? FixHint { get; set; }

    /// <summary>
    /// Additional parameters for the rule.
    /// </summary>
    [YamlMember(Alias = "parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// File patterns to exclude for this specific rule.
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Languages this rule applies to (empty = all languages).
    /// </summary>
    [YamlMember(Alias = "languages")]
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// Returns true if this rule is effectively enabled.
    /// </summary>
    public bool IsEnabled => Enabled && !Skip;

    /// <summary>
    /// Determines the validation type based on which configuration is provided.
    /// </summary>
    public RuleValidationType GetValidationType()
    {
        if (Validator != null)
            return RuleValidationType.Validator;
        if (Script != null)
            return RuleValidationType.Script;
        if (Expression != null)
            return RuleValidationType.Expression;
        if (Match != null)
            return RuleValidationType.MultiMatch;
        if (AstQuery != null)
            return RuleValidationType.AstQuery;
        if (PatternMatch != null || AntiPatternMatch != null)
            return RuleValidationType.EnhancedPattern;
        if (!string.IsNullOrEmpty(Pattern) || !string.IsNullOrEmpty(AntiPattern))
            return RuleValidationType.SimplePattern;
        if (!string.IsNullOrEmpty(CustomValidator))
            return RuleValidationType.CustomValidator;
        if (!string.IsNullOrEmpty(Id))
            return RuleValidationType.BuiltIn;

        return RuleValidationType.None;
    }
}

/// <summary>
/// Types of rule validation.
/// </summary>
public enum RuleValidationType
{
    /// <summary>No validation configured.</summary>
    None,
    /// <summary>Built-in analyzer by rule ID.</summary>
    BuiltIn,
    /// <summary>Simple regex pattern matching.</summary>
    SimplePattern,
    /// <summary>Enhanced pattern with captures and options.</summary>
    EnhancedPattern,
    /// <summary>AST node query matching.</summary>
    AstQuery,
    /// <summary>Multi-pattern AND/OR/NONE matching.</summary>
    MultiMatch,
    /// <summary>C# expression-based validation.</summary>
    Expression,
    /// <summary>Full C# script validation.</summary>
    Script,
    /// <summary>External validator reference.</summary>
    Validator,
    /// <summary>Custom validator from custom_validators section.</summary>
    CustomValidator
}
