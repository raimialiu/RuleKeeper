using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for referencing or defining a validator.
/// </summary>
public class ValidatorConfig
{
    /// <summary>
    /// Reference to a named validator defined in custom_validators section.
    /// </summary>
    [YamlMember(Alias = "reference")]
    public string? Reference { get; set; }

    /// <summary>
    /// Inline validator definition.
    /// </summary>
    [YamlMember(Alias = "inline")]
    public InlineValidatorConfig? Inline { get; set; }
}

/// <summary>
/// Inline validator definition.
/// </summary>
public class InlineValidatorConfig
{
    /// <summary>
    /// Type of validator: pattern, ast_query, expression, script, assembly.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "pattern";

    /// <summary>
    /// Pattern configuration (when type = pattern).
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public PatternConfig? Pattern { get; set; }

    /// <summary>
    /// AST query configuration (when type = ast_query).
    /// </summary>
    [YamlMember(Alias = "ast_query")]
    public AstQueryConfig? AstQuery { get; set; }

    /// <summary>
    /// Expression configuration (when type = expression).
    /// </summary>
    [YamlMember(Alias = "expression")]
    public ExpressionConfig? Expression { get; set; }

    /// <summary>
    /// Script configuration (when type = script).
    /// </summary>
    [YamlMember(Alias = "script")]
    public ScriptConfig? Script { get; set; }

    /// <summary>
    /// Assembly path (when type = assembly).
    /// </summary>
    [YamlMember(Alias = "assembly")]
    public string? Assembly { get; set; }

    /// <summary>
    /// Type name in assembly (when type = assembly).
    /// </summary>
    [YamlMember(Alias = "type_name")]
    public string? TypeName { get; set; }
}

/// <summary>
/// Enhanced custom validator configuration for the custom_validators section.
/// </summary>
public class EnhancedCustomValidatorConfig
{
    /// <summary>
    /// Description of what this validator checks.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of validator: pattern, ast_query, expression, script, assembly.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "pattern";

    /// <summary>
    /// Pattern configuration (when type = pattern).
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public PatternConfig? Pattern { get; set; }

    /// <summary>
    /// Simple regex pattern string (shorthand for pattern.regex).
    /// </summary>
    [YamlMember(Alias = "regex")]
    public string? Regex { get; set; }

    /// <summary>
    /// AST query configuration (when type = ast_query).
    /// </summary>
    [YamlMember(Alias = "ast_query")]
    public AstQueryConfig? AstQuery { get; set; }

    /// <summary>
    /// Expression configuration (when type = expression).
    /// </summary>
    [YamlMember(Alias = "expression")]
    public ExpressionConfig? Expression { get; set; }

    /// <summary>
    /// Script configuration (when type = script).
    /// </summary>
    [YamlMember(Alias = "script")]
    public ScriptConfig? Script { get; set; }

    /// <summary>
    /// Script code string (shorthand for script.code).
    /// </summary>
    [YamlMember(Alias = "script_code")]
    public string? ScriptCode { get; set; }

    /// <summary>
    /// Assembly path (when type = assembly).
    /// </summary>
    [YamlMember(Alias = "assembly")]
    public string? Assembly { get; set; }

    /// <summary>
    /// Type name in assembly (when type = assembly).
    /// </summary>
    [YamlMember(Alias = "type_name")]
    public string? TypeName { get; set; }

    /// <summary>
    /// Languages this validator supports.
    /// </summary>
    [YamlMember(Alias = "languages")]
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// Default severity for violations from this validator.
    /// </summary>
    [YamlMember(Alias = "severity")]
    public string? Severity { get; set; }

    /// <summary>
    /// Default message for violations.
    /// </summary>
    [YamlMember(Alias = "message")]
    public string? Message { get; set; }

    /// <summary>
    /// Additional parameters for the validator.
    /// </summary>
    [YamlMember(Alias = "parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}
