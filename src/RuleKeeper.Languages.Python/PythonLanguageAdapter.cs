using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Languages.Python;

/// <summary>
/// Language adapter for Python using ANTLR.
/// </summary>
public class PythonLanguageAdapter : ILanguageAdapter
{
    /// <inheritdoc />
    public Language Language => Language.Python;

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".py", ".pyw" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultIncludePatterns => new[] { "**/*.py" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultExcludePatterns => new[]
    {
        "**/venv/**",
        "**/__pycache__/**",
        "**/.env/**",
        "**/env/**",
        "**/.venv/**",
        "**/site-packages/**"
    };

    /// <inheritdoc />
    public async Task<IUnifiedSyntaxNode> ParseAsync(string source, string filePath, CancellationToken cancellationToken = default)
    {
        // For now, use a simple line-based parser until ANTLR grammar is integrated
        // This provides basic structural analysis
        return await Task.FromResult(new PythonSimpleNode(source, filePath));
    }

    /// <inheritdoc />
    public Task<ITypeResolver?> CreateTypeResolverAsync(IEnumerable<string> files, CancellationToken cancellationToken = default)
    {
        // Python type resolution would require additional type inference
        return Task.FromResult<ITypeResolver?>(null);
    }

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".py", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".pyw", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object? GetNativeContext(IUnifiedSyntaxNode root, string source, string filePath, ITypeResolver? typeResolver)
    {
        return null;
    }
}

/// <summary>
/// A simple Python syntax node implementation using basic parsing.
/// This is a placeholder until ANTLR grammar integration is complete.
/// </summary>
public class PythonSimpleNode : UnifiedSyntaxNodeBase
{
    private readonly string _source;
    private readonly string _filePath;
    private readonly List<PythonSimpleNode> _children;
    private readonly UnifiedSyntaxKind _kind;
    private readonly int _startLine;
    private readonly int _endLine;
    private readonly int _startColumn;
    private readonly int _endColumn;
    private readonly string _text;
    private readonly PythonSimpleNode? _parent;

    /// <summary>
    /// Creates a root Python node from source code.
    /// </summary>
    public PythonSimpleNode(string source, string filePath)
    {
        _source = source;
        _filePath = filePath;
        _kind = UnifiedSyntaxKind.CompilationUnit;
        _startLine = 1;
        _endLine = source.Split('\n').Length;
        _startColumn = 1;
        _endColumn = 1;
        _text = source;
        _parent = null;
        _children = ParseChildren(source, filePath);
    }

    /// <summary>
    /// Creates a child Python node.
    /// </summary>
    private PythonSimpleNode(
        UnifiedSyntaxKind kind,
        string text,
        string filePath,
        int startLine,
        int endLine,
        int startColumn,
        int endColumn,
        PythonSimpleNode parent)
    {
        _source = text;
        _filePath = filePath;
        _kind = kind;
        _startLine = startLine;
        _endLine = endLine;
        _startColumn = startColumn;
        _endColumn = endColumn;
        _text = text;
        _parent = parent;
        _children = new List<PythonSimpleNode>();
    }

    private List<PythonSimpleNode> ParseChildren(string source, string filePath)
    {
        var children = new List<PythonSimpleNode>();
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        int lineNumber = 1;
        bool inClass = false;
        bool inFunction = false;
        int classStartLine = 0;
        int funcStartLine = 0;
        int classIndent = 0;
        int funcIndent = 0;
        var classLines = new List<string>();
        var funcLines = new List<string>();
        string className = "";
        string funcName = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();
            var indent = line.Length - trimmedLine.Length;

            // Check for class definition
            if (trimmedLine.StartsWith("class "))
            {
                // Close any previous class/function
                if (inFunction && funcLines.Count > 0)
                {
                    children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    funcLines.Clear();
                    inFunction = false;
                }
                if (inClass && classLines.Count > 0)
                {
                    children.Add(CreateClassNode(className, classStartLine, lineNumber - 1, classLines, this));
                    classLines.Clear();
                }

                inClass = true;
                classStartLine = lineNumber;
                classIndent = indent;
                className = ExtractName(trimmedLine, "class ");
                classLines.Clear();
                classLines.Add(line);
            }
            // Check for function definition
            else if (trimmedLine.StartsWith("def ") || trimmedLine.StartsWith("async def "))
            {
                // Close any previous function
                if (inFunction && funcLines.Count > 0)
                {
                    if (inClass)
                    {
                        classLines.AddRange(funcLines);
                    }
                    else
                    {
                        children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    }
                    funcLines.Clear();
                }

                inFunction = true;
                funcStartLine = lineNumber;
                funcIndent = indent;
                funcName = ExtractName(trimmedLine, trimmedLine.StartsWith("async") ? "async def " : "def ");
                funcLines.Clear();
                funcLines.Add(line);
            }
            else
            {
                // Add to current context
                if (inFunction)
                {
                    funcLines.Add(line);
                }
                else if (inClass)
                {
                    classLines.Add(line);
                }
            }

            lineNumber++;
        }

        // Close any remaining open structures
        if (inFunction && funcLines.Count > 0)
        {
            if (inClass)
            {
                classLines.AddRange(funcLines);
            }
            else
            {
                children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
            }
        }
        if (inClass && classLines.Count > 0)
        {
            children.Add(CreateClassNode(className, classStartLine, lineNumber - 1, classLines, this));
        }

        return children;
    }

    private static string ExtractName(string line, string prefix)
    {
        var afterPrefix = line.Substring(line.IndexOf(prefix) + prefix.Length);
        var endIndex = afterPrefix.IndexOfAny(new[] { '(', ':', ' ' });
        return endIndex > 0 ? afterPrefix.Substring(0, endIndex) : afterPrefix.Trim();
    }

    private PythonSimpleNode CreateClassNode(string name, int startLine, int endLine, List<string> lines, PythonSimpleNode parent)
    {
        return new PythonSimpleNode(
            UnifiedSyntaxKind.ClassDeclaration,
            string.Join("\n", lines),
            _filePath,
            startLine,
            endLine,
            1,
            1,
            parent);
    }

    private PythonSimpleNode CreateMethodNode(string name, int startLine, int endLine, List<string> lines, PythonSimpleNode parent)
    {
        return new PythonSimpleNode(
            UnifiedSyntaxKind.MethodDeclaration,
            string.Join("\n", lines),
            _filePath,
            startLine,
            endLine,
            1,
            1,
            parent);
    }

    /// <inheritdoc />
    public override UnifiedSyntaxKind Kind => _kind;

    /// <inheritdoc />
    public override SourceLocation Location => new(
        _filePath,
        _startLine,
        _startColumn,
        _endLine,
        _endColumn);

    /// <inheritdoc />
    public override string Text => _text;

    /// <inheritdoc />
    public override IUnifiedSyntaxNode? Parent => _parent;

    /// <inheritdoc />
    public override IEnumerable<IUnifiedSyntaxNode> Children => _children;

    /// <inheritdoc />
    public override object? NativeNode => null;

    /// <inheritdoc />
    public override Language Language => Language.Python;

    /// <inheritdoc />
    public override bool ContainsErrors => false;
}
