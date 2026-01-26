using YamlDotNet.Serialization;

namespace RuleKeeper.Core.Configuration.Models;

/// <summary>
/// Configuration for querying AST nodes declaratively.
/// </summary>
public class AstQueryConfig
{
    /// <summary>
    /// Node kinds to match (e.g., MethodDeclaration, ClassDeclaration, FieldDeclaration).
    /// Maps to UnifiedSyntaxKind values.
    /// </summary>
    [YamlMember(Alias = "node_kinds")]
    public List<string> NodeKinds { get; set; } = new();

    /// <summary>
    /// Properties to match on the node.
    /// Example: { "IsPublic": true, "IsStatic": false }
    /// </summary>
    [YamlMember(Alias = "properties")]
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Parent node requirements.
    /// </summary>
    [YamlMember(Alias = "parent")]
    public AstQueryConfig? Parent { get; set; }

    /// <summary>
    /// Required child node kinds.
    /// </summary>
    [YamlMember(Alias = "has_children")]
    public List<string> HasChildren { get; set; } = new();

    /// <summary>
    /// Excluded child node kinds (node must NOT have these children).
    /// </summary>
    [YamlMember(Alias = "no_children")]
    public List<string> NoChildren { get; set; } = new();

    /// <summary>
    /// Required ancestor node kinds (at any level).
    /// </summary>
    [YamlMember(Alias = "has_ancestor")]
    public List<string> HasAncestor { get; set; } = new();

    /// <summary>
    /// Excluded ancestor node kinds (must NOT have these ancestors).
    /// </summary>
    [YamlMember(Alias = "no_ancestor")]
    public List<string> NoAncestor { get; set; } = new();

    /// <summary>
    /// Text content must match this regex.
    /// </summary>
    [YamlMember(Alias = "text_matches")]
    public string? TextMatches { get; set; }

    /// <summary>
    /// Text content must NOT match this regex.
    /// </summary>
    [YamlMember(Alias = "text_not_matches")]
    public string? TextNotMatches { get; set; }

    /// <summary>
    /// Name/identifier must match this regex.
    /// </summary>
    [YamlMember(Alias = "name_matches")]
    public string? NameMatches { get; set; }

    /// <summary>
    /// Name/identifier must NOT match this regex.
    /// </summary>
    [YamlMember(Alias = "name_not_matches")]
    public string? NameNotMatches { get; set; }

    /// <summary>
    /// Maximum depth in the AST tree (from root).
    /// </summary>
    [YamlMember(Alias = "max_depth")]
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Minimum depth in the AST tree (from root).
    /// </summary>
    [YamlMember(Alias = "min_depth")]
    public int? MinDepth { get; set; }

    /// <summary>
    /// Filter to specific languages.
    /// </summary>
    [YamlMember(Alias = "languages")]
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// Custom attribute requirements (for languages that support attributes/annotations).
    /// </summary>
    [YamlMember(Alias = "has_attribute")]
    public List<string> HasAttribute { get; set; } = new();

    /// <summary>
    /// Exclude nodes with these attributes.
    /// </summary>
    [YamlMember(Alias = "no_attribute")]
    public List<string> NoAttribute { get; set; } = new();

    /// <summary>
    /// Expression to evaluate for additional filtering.
    /// </summary>
    [YamlMember(Alias = "where")]
    public string? Where { get; set; }
}
