using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Languages.Java;

/// <summary>
/// Language adapter for Java using ANTLR.
/// </summary>
public class JavaLanguageAdapter : ILanguageAdapter
{
    /// <inheritdoc />
    public Language Language => Language.Java;

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".java" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultIncludePatterns => new[] { "**/*.java" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultExcludePatterns => new[]
    {
        "**/target/**",
        "**/build/**",
        "**/.gradle/**",
        "**/out/**",
        "**/bin/**"
    };

    /// <inheritdoc />
    public async Task<IUnifiedSyntaxNode> ParseAsync(string source, string filePath, CancellationToken cancellationToken = default)
    {
        // For now, use a simple line-based parser until ANTLR grammar is integrated
        return await Task.FromResult(new JavaSimpleNode(source, filePath));
    }

    /// <inheritdoc />
    public Task<ITypeResolver?> CreateTypeResolverAsync(IEnumerable<string> files, CancellationToken cancellationToken = default)
    {
        // Java type resolution would require a Java compiler API or Eclipse JDT
        return Task.FromResult<ITypeResolver?>(null);
    }

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".java", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object? GetNativeContext(IUnifiedSyntaxNode root, string source, string filePath, ITypeResolver? typeResolver)
    {
        return null;
    }
}

/// <summary>
/// A simple Java syntax node implementation using basic parsing.
/// This is a placeholder until ANTLR grammar integration is complete.
/// </summary>
public class JavaSimpleNode : UnifiedSyntaxNodeBase
{
    private readonly string _source;
    private readonly string _filePath;
    private readonly List<JavaSimpleNode> _children;
    private readonly UnifiedSyntaxKind _kind;
    private readonly int _startLine;
    private readonly int _endLine;
    private readonly int _startColumn;
    private readonly int _endColumn;
    private readonly string _text;
    private readonly JavaSimpleNode? _parent;

    /// <summary>
    /// Creates a root Java node from source code.
    /// </summary>
    public JavaSimpleNode(string source, string filePath)
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
    /// Creates a child Java node.
    /// </summary>
    private JavaSimpleNode(
        UnifiedSyntaxKind kind,
        string text,
        string filePath,
        int startLine,
        int endLine,
        int startColumn,
        int endColumn,
        JavaSimpleNode parent)
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
        _children = new List<JavaSimpleNode>();
    }

    private List<JavaSimpleNode> ParseChildren(string source, string filePath)
    {
        var children = new List<JavaSimpleNode>();
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        int lineNumber = 1;
        bool inClass = false;
        bool inInterface = false;
        bool inMethod = false;
        int structStartLine = 0;
        int methodStartLine = 0;
        int braceCount = 0;
        var structLines = new List<string>();
        var methodLines = new List<string>();
        string structName = "";
        string methodName = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();

            // Skip comments and empty lines for structure detection
            if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") ||
                trimmedLine.StartsWith("*") || string.IsNullOrWhiteSpace(trimmedLine))
            {
                if (inMethod)
                    methodLines.Add(line);
                else if (inClass || inInterface)
                    structLines.Add(line);
                lineNumber++;
                continue;
            }

            // Check for interface definition
            if (IsInterfaceDeclaration(trimmedLine))
            {
                if (inMethod && methodLines.Count > 0)
                {
                    children.Add(CreateMethodNode(methodName, methodStartLine, lineNumber - 1, methodLines, this));
                    methodLines.Clear();
                    inMethod = false;
                }
                if ((inClass || inInterface) && structLines.Count > 0)
                {
                    children.Add(CreateStructNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
                    structLines.Clear();
                }

                inInterface = true;
                inClass = false;
                structStartLine = lineNumber;
                structName = ExtractInterfaceName(trimmedLine);
                structLines.Clear();
                structLines.Add(line);
                braceCount = CountBraces(line);
            }
            // Check for class definition
            else if (IsClassDeclaration(trimmedLine))
            {
                if (inMethod && methodLines.Count > 0)
                {
                    children.Add(CreateMethodNode(methodName, methodStartLine, lineNumber - 1, methodLines, this));
                    methodLines.Clear();
                    inMethod = false;
                }
                if ((inClass || inInterface) && structLines.Count > 0)
                {
                    children.Add(CreateStructNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
                    structLines.Clear();
                }

                inClass = true;
                inInterface = false;
                structStartLine = lineNumber;
                structName = ExtractClassName(trimmedLine);
                structLines.Clear();
                structLines.Add(line);
                braceCount = CountBraces(line);
            }
            // Check for method definition
            else if (IsMethodDeclaration(trimmedLine) && (inClass || inInterface))
            {
                if (inMethod && methodLines.Count > 0)
                {
                    structLines.AddRange(methodLines);
                    methodLines.Clear();
                }

                inMethod = true;
                methodStartLine = lineNumber;
                methodName = ExtractMethodName(trimmedLine);
                methodLines.Clear();
                methodLines.Add(line);
                braceCount = CountBraces(line);
            }
            else
            {
                if (inMethod)
                {
                    methodLines.Add(line);
                    braceCount += CountBraces(line);
                    if (braceCount <= 0 && methodLines.Count > 1)
                    {
                        structLines.AddRange(methodLines);
                        methodLines.Clear();
                        inMethod = false;
                        braceCount = 0;
                    }
                }
                else if (inClass || inInterface)
                {
                    structLines.Add(line);
                }
            }

            lineNumber++;
        }

        // Close any remaining open structures
        if (inMethod && methodLines.Count > 0)
        {
            structLines.AddRange(methodLines);
        }
        if ((inClass || inInterface) && structLines.Count > 0)
        {
            children.Add(CreateStructNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
        }

        return children;
    }

    private static bool IsClassDeclaration(string line)
    {
        // Match patterns like "public class", "class", "abstract class", etc.
        var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Contains("class") && !tokens.Contains("interface");
    }

    private static bool IsInterfaceDeclaration(string line)
    {
        var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Contains("interface");
    }

    private static bool IsMethodDeclaration(string line)
    {
        // Method declarations typically have: modifier* returnType name(params) {
        if (!line.Contains("(") || !line.Contains(")"))
            return false;

        // Exclude control structures
        if (line.TrimStart().StartsWith("if") || line.TrimStart().StartsWith("for") ||
            line.TrimStart().StartsWith("while") || line.TrimStart().StartsWith("switch") ||
            line.TrimStart().StartsWith("catch") || line.TrimStart().StartsWith("try"))
            return false;

        // Check for method signature pattern
        var parenIndex = line.IndexOf('(');
        if (parenIndex > 0)
        {
            var beforeParen = line.Substring(0, parenIndex).Trim();
            var tokens = beforeParen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // Need at least return type and method name
            return tokens.Length >= 2 || (tokens.Length >= 1 && tokens[0] != "new");
        }

        return false;
    }

    private static string ExtractClassName(string line)
    {
        var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "class" && i + 1 < tokens.Length)
            {
                var className = tokens[i + 1];
                var endIndex = className.IndexOfAny(new[] { '<', '{', ' ' });
                return endIndex > 0 ? className.Substring(0, endIndex) : className;
            }
        }
        return "UnknownClass";
    }

    private static string ExtractInterfaceName(string line)
    {
        var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "interface" && i + 1 < tokens.Length)
            {
                var interfaceName = tokens[i + 1];
                var endIndex = interfaceName.IndexOfAny(new[] { '<', '{', ' ' });
                return endIndex > 0 ? interfaceName.Substring(0, endIndex) : interfaceName;
            }
        }
        return "UnknownInterface";
    }

    private static string ExtractMethodName(string line)
    {
        var parenIndex = line.IndexOf('(');
        if (parenIndex > 0)
        {
            var beforeParen = line.Substring(0, parenIndex).Trim();
            var tokens = beforeParen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0)
            {
                var methodName = tokens[^1];
                var genericIndex = methodName.IndexOf('<');
                return genericIndex > 0 ? methodName.Substring(0, genericIndex) : methodName;
            }
        }
        return "unknownMethod";
    }

    private static int CountBraces(string line)
    {
        int count = 0;
        bool inString = false;
        bool inChar = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inString)
            {
                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                    inString = false;
            }
            else if (inChar)
            {
                if (c == '\'' && (i == 0 || line[i - 1] != '\\'))
                    inChar = false;
            }
            else
            {
                if (c == '"')
                    inString = true;
                else if (c == '\'')
                    inChar = true;
                else if (c == '{')
                    count++;
                else if (c == '}')
                    count--;
            }
        }

        return count;
    }

    private JavaSimpleNode CreateStructNode(string name, int startLine, int endLine, List<string> lines, JavaSimpleNode parent, bool isInterface)
    {
        return new JavaSimpleNode(
            isInterface ? UnifiedSyntaxKind.InterfaceDeclaration : UnifiedSyntaxKind.ClassDeclaration,
            string.Join("\n", lines),
            _filePath,
            startLine,
            endLine,
            1,
            1,
            parent);
    }

    private JavaSimpleNode CreateMethodNode(string name, int startLine, int endLine, List<string> lines, JavaSimpleNode parent)
    {
        return new JavaSimpleNode(
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
    public override Language Language => Language.Java;

    /// <inheritdoc />
    public override bool ContainsErrors => false;
}
