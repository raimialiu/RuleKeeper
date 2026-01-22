namespace RuleKeeper.Sdk.Abstractions;

/// <summary>
/// Represents a language-agnostic source code location.
/// </summary>
public readonly struct SourceLocation : IEquatable<SourceLocation>
{
    /// <summary>
    /// Gets the file path where this location exists.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the starting line number (1-based).
    /// </summary>
    public int StartLine { get; }

    /// <summary>
    /// Gets the starting column number (1-based).
    /// </summary>
    public int StartColumn { get; }

    /// <summary>
    /// Gets the ending line number (1-based).
    /// </summary>
    public int EndLine { get; }

    /// <summary>
    /// Gets the ending column number (1-based).
    /// </summary>
    public int EndColumn { get; }

    /// <summary>
    /// Gets the starting character offset in the source text.
    /// </summary>
    public int StartOffset { get; }

    /// <summary>
    /// Gets the ending character offset in the source text.
    /// </summary>
    public int EndOffset { get; }

    /// <summary>
    /// Gets the length of the span in characters.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceLocation"/> struct.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="startLine">The starting line number (1-based).</param>
    /// <param name="startColumn">The starting column number (1-based).</param>
    /// <param name="endLine">The ending line number (1-based).</param>
    /// <param name="endColumn">The ending column number (1-based).</param>
    /// <param name="startOffset">The starting character offset.</param>
    /// <param name="endOffset">The ending character offset.</param>
    public SourceLocation(
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        int startOffset = 0,
        int endOffset = 0)
    {
        FilePath = filePath ?? string.Empty;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    /// <summary>
    /// Creates a location representing a single point.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <param name="offset">The character offset.</param>
    /// <returns>A new <see cref="SourceLocation"/>.</returns>
    public static SourceLocation FromPoint(string filePath, int line, int column, int offset = 0)
    {
        return new SourceLocation(filePath, line, column, line, column, offset, offset);
    }

    /// <summary>
    /// Creates a location spanning multiple lines.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="startLine">The starting line number (1-based).</param>
    /// <param name="startColumn">The starting column number (1-based).</param>
    /// <param name="endLine">The ending line number (1-based).</param>
    /// <param name="endColumn">The ending column number (1-based).</param>
    /// <returns>A new <see cref="SourceLocation"/>.</returns>
    public static SourceLocation FromLineSpan(
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        return new SourceLocation(filePath, startLine, startColumn, endLine, endColumn);
    }

    /// <summary>
    /// Creates a location from character offsets.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="startOffset">The starting character offset.</param>
    /// <param name="endOffset">The ending character offset.</param>
    /// <param name="sourceText">The source text for line/column calculation.</param>
    /// <returns>A new <see cref="SourceLocation"/>.</returns>
    public static SourceLocation FromOffsets(
        string filePath,
        int startOffset,
        int endOffset,
        string? sourceText = null)
    {
        int startLine = 1, startColumn = 1, endLine = 1, endColumn = 1;

        if (sourceText != null)
        {
            (startLine, startColumn) = CalculateLineAndColumn(sourceText, startOffset);
            (endLine, endColumn) = CalculateLineAndColumn(sourceText, endOffset);
        }

        return new SourceLocation(filePath, startLine, startColumn, endLine, endColumn, startOffset, endOffset);
    }

    private static (int line, int column) CalculateLineAndColumn(string text, int offset)
    {
        int line = 1;
        int column = 1;
        int currentOffset = 0;

        foreach (char c in text)
        {
            if (currentOffset >= offset)
                break;

            if (c == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            currentOffset++;
        }

        return (line, column);
    }

    /// <summary>
    /// Gets a location representing no location.
    /// </summary>
    public static SourceLocation None => new(string.Empty, 0, 0, 0, 0);

    /// <summary>
    /// Determines whether this location is valid.
    /// </summary>
    public bool IsValid => StartLine > 0 && StartColumn > 0;

    /// <inheritdoc />
    public bool Equals(SourceLocation other)
    {
        return FilePath == other.FilePath &&
               StartLine == other.StartLine &&
               StartColumn == other.StartColumn &&
               EndLine == other.EndLine &&
               EndColumn == other.EndColumn;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SourceLocation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(FilePath, StartLine, StartColumn, EndLine, EndColumn);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(SourceLocation left, SourceLocation right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(SourceLocation left, SourceLocation right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString()
    {
        if (!IsValid)
            return "(no location)";

        var position = StartLine == EndLine && StartColumn == EndColumn
            ? $"({StartLine},{StartColumn})"
            : $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";

        return string.IsNullOrEmpty(FilePath)
            ? position
            : $"{Path.GetFileName(FilePath)}{position}";
    }
}
