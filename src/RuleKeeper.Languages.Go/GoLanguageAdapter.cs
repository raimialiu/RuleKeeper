using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Languages.Go;

/// <summary>
/// Language adapter for Go using ANTLR.
/// </summary>
public class GoLanguageAdapter : ILanguageAdapter
{
    /// <inheritdoc />
    public Language Language => Language.Go;

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".go" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultIncludePatterns => new[] { "**/*.go" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultExcludePatterns => new[]
    {
        "**/vendor/**",
        "**/*_test.go",
        "**/testdata/**"
    };

    /// <inheritdoc />
    public async Task<IUnifiedSyntaxNode> ParseAsync(string source, string filePath, CancellationToken cancellationToken = default)
    {
        // For now, use a simple line-based parser until ANTLR grammar is integrated
        return await Task.FromResult(new GoSimpleNode(source, filePath));
    }

    /// <inheritdoc />
    public Task<ITypeResolver?> CreateTypeResolverAsync(IEnumerable<string> files, CancellationToken cancellationToken = default)
    {
        // Go type resolution would require go/types package
        return Task.FromResult<ITypeResolver?>(null);
    }

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".go", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object? GetNativeContext(IUnifiedSyntaxNode root, string source, string filePath, ITypeResolver? typeResolver)
    {
        return null;
    }
}

/// <summary>
/// A simple Go syntax node implementation using basic parsing.
/// This is a placeholder until ANTLR grammar integration is complete.
/// </summary>
public class GoSimpleNode : UnifiedSyntaxNodeBase
{
    private readonly string _source;
    private readonly string _filePath;
    private readonly List<GoSimpleNode> _children;
    private readonly UnifiedSyntaxKind _kind;
    private readonly int _startLine;
    private readonly int _endLine;
    private readonly int _startColumn;
    private readonly int _endColumn;
    private readonly string _text;
    private readonly GoSimpleNode? _parent;

    /// <summary>
    /// Creates a root Go node from source code.
    /// </summary>
    public GoSimpleNode(string source, string filePath)
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
    /// Creates a child Go node.
    /// </summary>
    private GoSimpleNode(
        UnifiedSyntaxKind kind,
        string text,
        string filePath,
        int startLine,
        int endLine,
        int startColumn,
        int endColumn,
        GoSimpleNode parent)
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
        _children = new List<GoSimpleNode>();
    }

    private List<GoSimpleNode> ParseChildren(string source, string filePath)
    {
        var children = new List<GoSimpleNode>();
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        int lineNumber = 1;
        bool inStruct = false;
        bool inInterface = false;
        bool inFunction = false;
        int structStartLine = 0;
        int funcStartLine = 0;
        int braceCount = 0;
        var structLines = new List<string>();
        var funcLines = new List<string>();
        string structName = "";
        string funcName = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();

            // Skip comments
            if (trimmedLine.StartsWith("//") || string.IsNullOrWhiteSpace(trimmedLine))
            {
                if (inFunction)
                    funcLines.Add(line);
                else if (inStruct || inInterface)
                    structLines.Add(line);
                lineNumber++;
                continue;
            }

            // Check for struct definition
            if (trimmedLine.StartsWith("type ") && trimmedLine.Contains(" struct"))
            {
                if (inFunction && funcLines.Count > 0)
                {
                    children.Add(CreateFunctionNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    funcLines.Clear();
                    inFunction = false;
                }
                if ((inStruct || inInterface) && structLines.Count > 0)
                {
                    children.Add(CreateTypeNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
                    structLines.Clear();
                }

                inStruct = true;
                inInterface = false;
                structStartLine = lineNumber;
                structName = ExtractTypeName(trimmedLine, "struct");
                structLines.Clear();
                structLines.Add(line);
                braceCount = CountBraces(line);
            }
            // Check for interface definition
            else if (trimmedLine.StartsWith("type ") && trimmedLine.Contains(" interface"))
            {
                if (inFunction && funcLines.Count > 0)
                {
                    children.Add(CreateFunctionNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    funcLines.Clear();
                    inFunction = false;
                }
                if ((inStruct || inInterface) && structLines.Count > 0)
                {
                    children.Add(CreateTypeNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
                    structLines.Clear();
                }

                inInterface = true;
                inStruct = false;
                structStartLine = lineNumber;
                structName = ExtractTypeName(trimmedLine, "interface");
                structLines.Clear();
                structLines.Add(line);
                braceCount = CountBraces(line);
            }
            // Check for function definition
            else if (trimmedLine.StartsWith("func "))
            {
                if (inFunction && funcLines.Count > 0)
                {
                    children.Add(CreateFunctionNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    funcLines.Clear();
                }
                if ((inStruct || inInterface) && structLines.Count > 0)
                {
                    children.Add(CreateTypeNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
                    structLines.Clear();
                    inStruct = false;
                    inInterface = false;
                }

                inFunction = true;
                funcStartLine = lineNumber;
                funcName = ExtractFunctionName(trimmedLine);
                funcLines.Clear();
                funcLines.Add(line);
                braceCount = CountBraces(line);
            }
            else
            {
                if (inFunction)
                {
                    funcLines.Add(line);
                    braceCount += CountBraces(line);
                    if (braceCount <= 0 && funcLines.Count > 1)
                    {
                        children.Add(CreateFunctionNode(funcName, funcStartLine, lineNumber, funcLines, this));
                        funcLines.Clear();
                        inFunction = false;
                        braceCount = 0;
                    }
                }
                else if (inStruct || inInterface)
                {
                    structLines.Add(line);
                    braceCount += CountBraces(line);
                    if (braceCount <= 0 && structLines.Count > 1)
                    {
                        children.Add(CreateTypeNode(structName, structStartLine, lineNumber, structLines, this, inInterface));
                        structLines.Clear();
                        inStruct = false;
                        inInterface = false;
                        braceCount = 0;
                    }
                }
            }

            lineNumber++;
        }

        // Close any remaining open structures
        if (inFunction && funcLines.Count > 0)
        {
            children.Add(CreateFunctionNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
        }
        if ((inStruct || inInterface) && structLines.Count > 0)
        {
            children.Add(CreateTypeNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
        }

        return children;
    }

    private static string ExtractTypeName(string line, string typeKeyword)
    {
        // Pattern: type Name struct/interface {
        var typeIndex = line.IndexOf("type ");
        if (typeIndex >= 0)
        {
            var afterType = line.Substring(typeIndex + 5).TrimStart();
            var keywordIndex = afterType.IndexOf(" " + typeKeyword);
            if (keywordIndex > 0)
            {
                return afterType.Substring(0, keywordIndex).Trim();
            }
        }
        return "UnknownType";
    }

    private static string ExtractFunctionName(string line)
    {
        // Pattern: func name( or func (receiver) name(
        var funcIndex = line.IndexOf("func ");
        if (funcIndex >= 0)
        {
            var afterFunc = line.Substring(funcIndex + 5).TrimStart();

            // Check for receiver (method)
            if (afterFunc.StartsWith("("))
            {
                var receiverEnd = afterFunc.IndexOf(')');
                if (receiverEnd > 0)
                {
                    afterFunc = afterFunc.Substring(receiverEnd + 1).TrimStart();
                }
            }

            var parenIndex = afterFunc.IndexOf('(');
            if (parenIndex > 0)
            {
                return afterFunc.Substring(0, parenIndex).Trim();
            }
        }
        return "unknownFunc";
    }

    private static int CountBraces(string line)
    {
        int count = 0;
        bool inString = false;
        bool inRune = false;
        bool inRawString = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inRawString)
            {
                if (c == '`')
                    inRawString = false;
            }
            else if (inString)
            {
                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                    inString = false;
            }
            else if (inRune)
            {
                if (c == '\'' && (i == 0 || line[i - 1] != '\\'))
                    inRune = false;
            }
            else
            {
                if (c == '`')
                    inRawString = true;
                else if (c == '"')
                    inString = true;
                else if (c == '\'')
                    inRune = true;
                else if (c == '{')
                    count++;
                else if (c == '}')
                    count--;
            }
        }

        return count;
    }

    private GoSimpleNode CreateTypeNode(string name, int startLine, int endLine, List<string> lines, GoSimpleNode parent, bool isInterface)
    {
        return new GoSimpleNode(
            isInterface ? UnifiedSyntaxKind.InterfaceDeclaration : UnifiedSyntaxKind.StructDeclaration,
            string.Join("\n", lines),
            _filePath,
            startLine,
            endLine,
            1,
            1,
            parent);
    }

    private GoSimpleNode CreateFunctionNode(string name, int startLine, int endLine, List<string> lines, GoSimpleNode parent)
    {
        return new GoSimpleNode(
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
    public override Language Language => Language.Go;

    /// <inheritdoc />
    public override bool ContainsErrors => false;
}
