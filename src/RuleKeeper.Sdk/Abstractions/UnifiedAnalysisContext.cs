namespace RuleKeeper.Sdk.Abstractions;

/// <summary>
/// Language-agnostic context for analyzing source code.
/// This context can be used by cross-language rules that work with the unified AST.
/// </summary>
public class UnifiedAnalysisContext
{
    /// <summary>
    /// Gets or sets the root node of the syntax tree.
    /// </summary>
    public required IUnifiedSyntaxNode Root { get; init; }

    /// <summary>
    /// Gets or sets the programming language of the source code.
    /// </summary>
    public required Language Language { get; init; }

    /// <summary>
    /// Gets or sets the file path of the source file being analyzed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the raw source text of the file.
    /// </summary>
    public required string SourceText { get; init; }

    /// <summary>
    /// Gets the lines of the source text.
    /// </summary>
    public IReadOnlyList<string> Lines => _lines ??= SourceText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    private IReadOnlyList<string>? _lines;

    /// <summary>
    /// Gets or sets the severity level to use for violations.
    /// </summary>
    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    /// <summary>
    /// Gets or sets a custom message override for violations.
    /// </summary>
    public string? CustomMessage { get; init; }

    /// <summary>
    /// Gets or sets a fix hint for violations.
    /// </summary>
    public string? FixHint { get; init; }

    /// <summary>
    /// Gets or sets the cancellation token for the analysis.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// Gets or sets the type resolver for semantic analysis, if available.
    /// </summary>
    public ITypeResolver? TypeResolver { get; init; }

    /// <summary>
    /// Gets or sets additional metadata for the analysis context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets all class declarations in the file.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetClasses() =>
        Root.DescendantsOfKind(UnifiedSyntaxKind.ClassDeclaration);

    /// <summary>
    /// Gets all method declarations in the file.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetMethods() =>
        Root.DescendantsOfKind(UnifiedSyntaxKind.MethodDeclaration);

    /// <summary>
    /// Gets all interface declarations in the file.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetInterfaces() =>
        Root.DescendantsOfKind(UnifiedSyntaxKind.InterfaceDeclaration);

    /// <summary>
    /// Gets all property declarations in the file.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetProperties() =>
        Root.DescendantsOfKind(UnifiedSyntaxKind.PropertyDeclaration);

    /// <summary>
    /// Gets all field declarations in the file.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetFields() =>
        Root.DescendantsOfKind(UnifiedSyntaxKind.FieldDeclaration);

    /// <summary>
    /// Gets all invocation expressions in the file.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetInvocations() =>
        Root.DescendantsOfKind(UnifiedSyntaxKind.InvocationExpression);

    /// <summary>
    /// Gets the source text for a specific line (0-based index).
    /// </summary>
    /// <param name="lineIndex">The 0-based line index.</param>
    /// <returns>The line text, or empty string if out of range.</returns>
    public string GetLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= Lines.Count)
            return string.Empty;
        return Lines[lineIndex];
    }

    /// <summary>
    /// Gets the source text for a location.
    /// </summary>
    /// <param name="location">The source location.</param>
    /// <returns>The source text at the location.</returns>
    public string GetTextAtLocation(SourceLocation location)
    {
        if (!location.IsValid)
            return string.Empty;

        if (location.StartOffset > 0 && location.EndOffset > 0)
        {
            return SourceText.Substring(location.StartOffset, location.Length);
        }

        // Fall back to line-based extraction
        if (location.StartLine == location.EndLine)
        {
            var line = GetLine(location.StartLine - 1);
            var startCol = Math.Min(location.StartColumn - 1, line.Length);
            var endCol = Math.Min(location.EndColumn - 1, line.Length);
            return line.Substring(startCol, endCol - startCol);
        }

        // Multi-line extraction
        var result = new List<string>();
        for (int i = location.StartLine - 1; i < location.EndLine && i < Lines.Count; i++)
        {
            var line = Lines[i];
            if (i == location.StartLine - 1)
                result.Add(line.Substring(Math.Min(location.StartColumn - 1, line.Length)));
            else if (i == location.EndLine - 1)
                result.Add(line.Substring(0, Math.Min(location.EndColumn - 1, line.Length)));
            else
                result.Add(line);
        }
        return string.Join(Environment.NewLine, result);
    }

    /// <summary>
    /// Gets the line count for a node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="countBlankLines">Whether to count blank lines.</param>
    /// <param name="countComments">Whether to count comment lines.</param>
    /// <returns>The number of lines.</returns>
    public int GetLineCount(IUnifiedSyntaxNode node, bool countBlankLines = true, bool countComments = true)
    {
        var location = node.Location;
        if (!location.IsValid)
            return 0;

        if (countBlankLines && countComments)
            return location.EndLine - location.StartLine + 1;

        int count = 0;
        for (int i = location.StartLine - 1; i < location.EndLine && i < Lines.Count; i++)
        {
            var line = Lines[i].Trim();

            if (!countBlankLines && string.IsNullOrWhiteSpace(line))
                continue;

            if (!countComments && IsCommentLine(line))
                continue;

            count++;
        }

        return count;
    }

    private bool IsCommentLine(string line)
    {
        return Language switch
        {
            Language.CSharp or Language.Java or Language.JavaScript or Language.TypeScript or Language.Go =>
                line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*"),
            Language.Python =>
                line.StartsWith("#") || line.StartsWith("'''") || line.StartsWith("\"\"\""),
            _ => line.StartsWith("//") || line.StartsWith("#")
        };
    }
}
