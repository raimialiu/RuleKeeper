using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Unified validation context that provides access to source code, AST, and metadata.
/// Works with both language-specific (Roslyn) and unified (cross-language) ASTs.
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// The unique identifier for the rule being executed.
    /// </summary>
    public string? RuleId { get; init; }

    /// <summary>
    /// The display name for the rule being executed.
    /// </summary>
    public string? RuleName { get; init; }

    /// <summary>
    /// The file path being analyzed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The programming language of the source code.
    /// </summary>
    public Language Language { get; init; } = Language.CSharp;

    /// <summary>
    /// The raw source text.
    /// </summary>
    public required string SourceText { get; init; }

    /// <summary>
    /// Roslyn syntax tree (for C# and VB).
    /// </summary>
    public SyntaxTree? RoslynSyntaxTree { get; init; }

    /// <summary>
    /// Roslyn semantic model (for C# and VB).
    /// </summary>
    public SemanticModel? RoslynSemanticModel { get; init; }

    /// <summary>
    /// Roslyn compilation (for C# and VB).
    /// </summary>
    public CSharpCompilation? RoslynCompilation { get; init; }

    /// <summary>
    /// Unified syntax tree root (for cross-language analysis).
    /// </summary>
    public IUnifiedSyntaxNode? UnifiedRoot { get; init; }

    /// <summary>
    /// Type resolver for semantic analysis (cross-language).
    /// </summary>
    public Sdk.Abstractions.ITypeResolver? TypeResolver { get; init; }

    /// <summary>
    /// The severity level for violations.
    /// </summary>
    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    /// <summary>
    /// Custom message override.
    /// </summary>
    public string? CustomMessage { get; init; }

    /// <summary>
    /// Fix hint for violations.
    /// </summary>
    public string? FixHint { get; init; }

    /// <summary>
    /// Additional parameters from the rule definition.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// Additional metadata that can be used by validators.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Source text split into lines (lazily computed).
    /// </summary>
    private string[]? _lines;
    public string[] Lines => _lines ??= SourceText.Split('\n');

    /// <summary>
    /// Get a specific line (1-indexed).
    /// </summary>
    public string? GetLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > Lines.Length)
            return null;
        return Lines[lineNumber - 1];
    }

    /// <summary>
    /// Get the total number of lines.
    /// </summary>
    public int LineCount => Lines.Length;

    /// <summary>
    /// Creates a validation context from a Roslyn AnalysisContext.
    /// </summary>
    public static ValidationContext FromRoslynContext(Sdk.AnalysisContext roslynContext, string? ruleId = null, string? ruleName = null)
    {
        return new ValidationContext
        {
            RuleId = ruleId,
            RuleName = ruleName,
            FilePath = roslynContext.FilePath,
            Language = roslynContext.Language,
            SourceText = roslynContext.SyntaxTree.GetText().ToString(),
            RoslynSyntaxTree = roslynContext.SyntaxTree,
            RoslynSemanticModel = roslynContext.SemanticModel,
            RoslynCompilation = roslynContext.Compilation,
            Severity = roslynContext.Severity,
            CustomMessage = roslynContext.CustomMessage,
            FixHint = roslynContext.FixHint
        };
    }

    /// <summary>
    /// Creates a validation context from a UnifiedAnalysisContext.
    /// </summary>
    public static ValidationContext FromUnifiedContext(UnifiedAnalysisContext unifiedContext, string? ruleId = null, string? ruleName = null)
    {
        return new ValidationContext
        {
            RuleId = ruleId,
            RuleName = ruleName,
            FilePath = unifiedContext.FilePath,
            Language = unifiedContext.Language,
            SourceText = unifiedContext.SourceText,
            UnifiedRoot = unifiedContext.Root,
            TypeResolver = unifiedContext.TypeResolver,
            Severity = unifiedContext.Severity,
            CustomMessage = unifiedContext.CustomMessage,
            FixHint = unifiedContext.FixHint
        };
    }

    /// <summary>
    /// Creates a child context with overridden properties.
    /// </summary>
    public ValidationContext WithOverrides(
        SeverityLevel? severity = null,
        string? message = null,
        string? fixHint = null,
        Dictionary<string, object>? additionalParameters = null)
    {
        var parameters = new Dictionary<string, object>(Parameters);
        if (additionalParameters != null)
        {
            foreach (var kvp in additionalParameters)
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }

        return new ValidationContext
        {
            RuleId = RuleId,
            RuleName = RuleName,
            FilePath = FilePath,
            Language = Language,
            SourceText = SourceText,
            RoslynSyntaxTree = RoslynSyntaxTree,
            RoslynSemanticModel = RoslynSemanticModel,
            RoslynCompilation = RoslynCompilation,
            UnifiedRoot = UnifiedRoot,
            TypeResolver = TypeResolver,
            Severity = severity ?? Severity,
            CustomMessage = message ?? CustomMessage,
            FixHint = fixHint ?? FixHint,
            Parameters = parameters,
            Metadata = new Dictionary<string, object>(Metadata)
        };
    }
}

