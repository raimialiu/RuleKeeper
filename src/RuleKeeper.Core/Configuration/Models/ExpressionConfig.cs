using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for C# expression-based validation.
/// Expressions are evaluated using Roslyn scripting.
/// </summary>
public class ExpressionConfig
{
    /// <summary>
    /// The C# expression that returns bool.
    /// Available variables: Node, Context, Parameters, SourceText, FilePath, Language
    /// Example: "Node is IMethodNode m && m.Parameters.Count > 5"
    /// </summary>
    [YamlMember(Alias = "condition")]
    public string? Condition { get; set; }

    /// <summary>
    /// C# expression that returns the violation message.
    /// Available variables: same as condition, plus Match (if pattern matched).
    /// Example: "$\"Method {Node.Name} has too many parameters ({((IMethodNode)Node).Parameters.Count})\""
    /// </summary>
    [YamlMember(Alias = "message_expression")]
    public string? MessageExpression { get; set; }

    /// <summary>
    /// C# expression that returns the severity level.
    /// Example: "Parameters.Count > 10 ? SeverityLevel.Critical : SeverityLevel.Medium"
    /// </summary>
    [YamlMember(Alias = "severity_expression")]
    public string? SeverityExpression { get; set; }

    /// <summary>
    /// Additional using statements to include.
    /// </summary>
    [YamlMember(Alias = "usings")]
    public List<string> Usings { get; set; } = new();

    /// <summary>
    /// Additional assembly references (by name).
    /// </summary>
    [YamlMember(Alias = "references")]
    public List<string> References { get; set; } = new();

    /// <summary>
    /// Whether to cache the compiled expression.
    /// Default is true for performance.
    /// </summary>
    [YamlMember(Alias = "cache")]
    public bool Cache { get; set; } = true;
}

/// <summary>
/// Configuration for full C# script-based validation.
/// Scripts have more capabilities than expressions but are heavier.
/// </summary>
public class ScriptConfig
{
    /// <summary>
    /// The full C# script code.
    /// Must return IEnumerable&lt;Violation&gt; or void (use Context.AddViolation).
    /// Available: Context, Node, Parameters, SourceText, FilePath, Language, CreateViolation()
    /// </summary>
    [YamlMember(Alias = "code")]
    public string? Code { get; set; }

    /// <summary>
    /// Path to external script file (alternative to inline code).
    /// </summary>
    [YamlMember(Alias = "file")]
    public string? File { get; set; }

    /// <summary>
    /// Additional using statements.
    /// </summary>
    [YamlMember(Alias = "usings")]
    public List<string> Usings { get; set; } = new();

    /// <summary>
    /// Additional assembly references.
    /// </summary>
    [YamlMember(Alias = "references")]
    public List<string> References { get; set; } = new();

    /// <summary>
    /// Entry point method name (default is "Validate").
    /// </summary>
    [YamlMember(Alias = "entry_point")]
    public string EntryPoint { get; set; } = "Validate";

    /// <summary>
    /// Script timeout in milliseconds.
    /// </summary>
    [YamlMember(Alias = "timeout_ms")]
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Whether to cache the compiled script.
    /// </summary>
    [YamlMember(Alias = "cache")]
    public bool Cache { get; set; } = true;
}
