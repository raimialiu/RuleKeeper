using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RuleKeeper.Sdk.CSharp;

/// <summary>
/// Context for analyzing a C# source file using Roslyn.
/// </summary>
public record CSharpAnalysisContext
{
    /// <summary>
    /// The syntax tree of the file being analyzed.
    /// </summary>
    public required SyntaxTree SyntaxTree { get; init; }

    /// <summary>
    /// The semantic model for the file, if available.
    /// </summary>
    public SemanticModel? SemanticModel { get; init; }

    /// <summary>
    /// The compilation containing the file, if available.
    /// </summary>
    public CSharpCompilation? Compilation { get; init; }

    /// <summary>
    /// The file path of the file being analyzed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The severity level to use for violations.
    /// </summary>
    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    /// <summary>
    /// Custom message override for violations.
    /// </summary>
    public string? CustomMessage { get; init; }

    /// <summary>
    /// Fix hint for violations.
    /// </summary>
    public string? FixHint { get; init; }

    /// <summary>
    /// Cancellation token for the analysis.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}
