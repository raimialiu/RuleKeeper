using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Languages.JavaScript;

/// <summary>
/// Language adapter for TypeScript using ANTLR.
/// </summary>
public class TypeScriptLanguageAdapter : ILanguageAdapter
{
    /// <inheritdoc />
    public Language Language => Language.TypeScript;

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".ts", ".tsx", ".mts", ".cts" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultIncludePatterns => new[] { "**/*.ts", "**/*.tsx", "**/*.mts" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultExcludePatterns => new[]
    {
        "**/node_modules/**",
        "**/dist/**",
        "**/build/**",
        "**/.next/**",
        "**/coverage/**",
        "**/*.d.ts"
    };

    /// <inheritdoc />
    public async Task<IUnifiedSyntaxNode> ParseAsync(string source, string filePath, CancellationToken cancellationToken = default)
    {
        // For now, use a simple line-based parser until ANTLR grammar is integrated
        return await Task.FromResult(new TypeScriptSimpleNode(source, filePath));
    }

    /// <inheritdoc />
    public Task<ITypeResolver?> CreateTypeResolverAsync(IEnumerable<string> files, CancellationToken cancellationToken = default)
    {
        // TypeScript type resolution would require the TypeScript compiler API
        return Task.FromResult<ITypeResolver?>(null);
    }

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".ts", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tsx", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".mts", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".cts", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object? GetNativeContext(IUnifiedSyntaxNode root, string source, string filePath, ITypeResolver? typeResolver)
    {
        return null;
    }
}

/// <summary>
/// A simple TypeScript syntax node implementation using basic parsing.
/// This is a placeholder until ANTLR grammar integration is complete.
/// </summary>
public class TypeScriptSimpleNode : UnifiedSyntaxNodeBase
{
    private readonly string _source;
    private readonly string _filePath;
    private readonly List<TypeScriptSimpleNode> _children;
    private readonly UnifiedSyntaxKind _kind;
    private readonly int _startLine;
    private readonly int _endLine;
    private readonly int _startColumn;
    private readonly int _endColumn;
    private readonly string _text;
    private readonly TypeScriptSimpleNode? _parent;

    /// <summary>
    /// Creates a root TypeScript node from source code.
    /// </summary>
    public TypeScriptSimpleNode(string source, string filePath)
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
    /// Creates a child TypeScript node.
    /// </summary>
    private TypeScriptSimpleNode(
        UnifiedSyntaxKind kind,
        string text,
        string filePath,
        int startLine,
        int endLine,
        int startColumn,
        int endColumn,
        TypeScriptSimpleNode parent)
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
        _children = new List<TypeScriptSimpleNode>();
    }

    private List<TypeScriptSimpleNode> ParseChildren(string source, string filePath)
    {
        var children = new List<TypeScriptSimpleNode>();
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        int lineNumber = 1;
        bool inClass = false;
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

            // Check for interface definition
            if (trimmedLine.StartsWith("interface ") || trimmedLine.StartsWith("export interface "))
            {
                if (inFunction && funcLines.Count > 0)
                {
                    children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    funcLines.Clear();
                    inFunction = false;
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
            else if (trimmedLine.StartsWith("class ") || trimmedLine.StartsWith("export class ") ||
                     trimmedLine.StartsWith("abstract class ") || trimmedLine.StartsWith("export abstract class "))
            {
                if (inFunction && funcLines.Count > 0)
                {
                    children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    funcLines.Clear();
                    inFunction = false;
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
            // Check for function definition
            else if (IsFunctionDeclaration(trimmedLine))
            {
                if (inFunction && funcLines.Count > 0)
                {
                    if (inClass || inInterface)
                    {
                        structLines.AddRange(funcLines);
                    }
                    else
                    {
                        children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
                    }
                    funcLines.Clear();
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
                        if (inClass || inInterface)
                        {
                            structLines.AddRange(funcLines);
                        }
                        else
                        {
                            children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber, funcLines, this));
                        }
                        funcLines.Clear();
                        inFunction = false;
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
        if (inFunction && funcLines.Count > 0)
        {
            if (inClass || inInterface)
            {
                structLines.AddRange(funcLines);
            }
            else
            {
                children.Add(CreateMethodNode(funcName, funcStartLine, lineNumber - 1, funcLines, this));
            }
        }
        if ((inClass || inInterface) && structLines.Count > 0)
        {
            children.Add(CreateStructNode(structName, structStartLine, lineNumber - 1, structLines, this, inInterface));
        }

        return children;
    }

    private static bool IsFunctionDeclaration(string line)
    {
        return line.StartsWith("function ") ||
               line.StartsWith("async function ") ||
               line.StartsWith("export function ") ||
               line.StartsWith("export async function ") ||
               line.Contains("=> {") ||
               (line.Contains("(") && line.Contains(")") && line.Contains("{") &&
                !line.StartsWith("if") && !line.StartsWith("for") && !line.StartsWith("while"));
    }

    private static string ExtractInterfaceName(string line)
    {
        var interfaceIndex = line.IndexOf("interface ");
        if (interfaceIndex >= 0)
        {
            var afterInterface = line.Substring(interfaceIndex + 10).TrimStart();
            var endIndex = afterInterface.IndexOfAny(new[] { ' ', '{', '<' });
            return endIndex > 0 ? afterInterface.Substring(0, endIndex) : afterInterface.Trim();
        }
        return "UnknownInterface";
    }

    private static string ExtractClassName(string line)
    {
        var classIndex = line.IndexOf("class ");
        if (classIndex >= 0)
        {
            var afterClass = line.Substring(classIndex + 6).TrimStart();
            var endIndex = afterClass.IndexOfAny(new[] { ' ', '{', '<', '(' });
            return endIndex > 0 ? afterClass.Substring(0, endIndex) : afterClass.Trim();
        }
        return "UnknownClass";
    }

    private static string ExtractFunctionName(string line)
    {
        // Handle "function name(" pattern
        if (line.Contains("function "))
        {
            var funcIndex = line.IndexOf("function ");
            var afterFunc = line.Substring(funcIndex + 9).TrimStart();
            var endIndex = afterFunc.IndexOfAny(new[] { '(', '<' });
            if (endIndex > 0)
                return afterFunc.Substring(0, endIndex).Trim();
        }

        // Handle arrow functions
        var assignIndex = line.IndexOf('=');
        if (assignIndex > 0)
        {
            var beforeAssign = line.Substring(0, assignIndex).Trim();
            var parts = beforeAssign.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return parts[^1].TrimEnd(':');
        }

        // Handle method shorthand
        var parenIndex = line.IndexOf('(');
        if (parenIndex > 0)
        {
            var beforeParen = line.Substring(0, parenIndex).Trim();
            var parts = beforeParen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
                return parts[^1];
        }

        return "anonymous";
    }

    private static int CountBraces(string line)
    {
        int count = 0;
        bool inString = false;
        char stringChar = '\0';

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inString)
            {
                if (c == stringChar && (i == 0 || line[i - 1] != '\\'))
                    inString = false;
            }
            else
            {
                if (c == '"' || c == '\'' || c == '`')
                {
                    inString = true;
                    stringChar = c;
                }
                else if (c == '{')
                    count++;
                else if (c == '}')
                    count--;
            }
        }

        return count;
    }

    private TypeScriptSimpleNode CreateStructNode(string name, int startLine, int endLine, List<string> lines, TypeScriptSimpleNode parent, bool isInterface)
    {
        return new TypeScriptSimpleNode(
            isInterface ? UnifiedSyntaxKind.InterfaceDeclaration : UnifiedSyntaxKind.ClassDeclaration,
            string.Join("\n", lines),
            _filePath,
            startLine,
            endLine,
            1,
            1,
            parent);
    }

    private TypeScriptSimpleNode CreateMethodNode(string name, int startLine, int endLine, List<string> lines, TypeScriptSimpleNode parent)
    {
        return new TypeScriptSimpleNode(
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
    public override Language Language => Language.TypeScript;

    /// <inheritdoc />
    public override bool ContainsErrors => false;
}
