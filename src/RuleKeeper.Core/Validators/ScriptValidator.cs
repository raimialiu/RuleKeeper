using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Validator that uses full C# scripts for complex validation logic.
/// </summary>
public class ScriptValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _name;
    private readonly string? _description;
    private readonly ScriptConfig _script;
    private readonly string? _defaultMessage;

    private static readonly ConcurrentDictionary<string, Script<IEnumerable<Violation>>> _scriptCache = new();

    public override string ValidatorId => _validatorId;
    public override string Name => _name;
    public override string? Description => _description;

    public ScriptValidator(
        string validatorId,
        string name,
        string? description,
        ScriptConfig script,
        string? defaultMessage = null)
    {
        _validatorId = validatorId;
        _name = name;
        _description = description;
        _script = script;
        _defaultMessage = defaultMessage;
    }

    public override async Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var scriptCode = await GetScriptCodeAsync();
            if (string.IsNullOrEmpty(scriptCode))
                return Enumerable.Empty<Violation>();

            var globals = new ScriptGlobals(context, _validatorId, _name, _defaultMessage);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_script.TimeoutMs);

            var violations = await ExecuteScriptAsync(scriptCode, globals, cts.Token);
            return violations ?? Enumerable.Empty<Violation>();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new[]
            {
                CreateViolation(context, $"Script timed out after {_script.TimeoutMs}ms", 1, 1)
            };
        }
        catch (CompilationErrorException ex)
        {
            return new[]
            {
                CreateViolation(context,
                    $"Script compilation error: {string.Join(", ", ex.Diagnostics.Select(d => d.GetMessage()))}",
                    1, 1)
            };
        }
        catch (Exception ex)
        {
            return new[]
            {
                CreateViolation(context, $"Script execution error: {ex.Message}", 1, 1)
            };
        }
    }

    private async Task<string?> GetScriptCodeAsync()
    {
        if (!string.IsNullOrEmpty(_script.Code))
            return _script.Code;

        if (!string.IsNullOrEmpty(_script.File) && File.Exists(_script.File))
            return await File.ReadAllTextAsync(_script.File);

        return null;
    }

    private async Task<IEnumerable<Violation>> ExecuteScriptAsync(string code, ScriptGlobals globals, CancellationToken cancellationToken)
    {
        var cacheKey = _script.Cache ? code.GetHashCode().ToString() : null;
        Script<IEnumerable<Violation>>? script = null;

        if (cacheKey != null && _scriptCache.TryGetValue(cacheKey, out script))
        {
            var result = await script.RunAsync(globals, cancellationToken);
            return result.ReturnValue ?? Enumerable.Empty<Violation>();
        }

        var options = CreateScriptOptions();
        script = CSharpScript.Create<IEnumerable<Violation>>(code, options, typeof(ScriptGlobals));

        if (cacheKey != null)
        {
            _scriptCache.TryAdd(cacheKey, script);
        }

        var runResult = await script.RunAsync(globals, cancellationToken);
        return runResult.ReturnValue ?? Enumerable.Empty<Violation>();
    }

    private ScriptOptions CreateScriptOptions()
    {
        var options = ScriptOptions.Default
            .AddReferences(typeof(object).Assembly)
            .AddReferences(typeof(Enumerable).Assembly)
            .AddReferences(typeof(List<>).Assembly)
            .AddReferences(typeof(IUnifiedSyntaxNode).Assembly)
            .AddReferences(typeof(Violation).Assembly)
            .AddImports("System")
            .AddImports("System.Linq")
            .AddImports("System.Collections.Generic")
            .AddImports("System.Text.RegularExpressions")
            .AddImports("RuleKeeper.Sdk")
            .AddImports("RuleKeeper.Sdk.Abstractions")
            .AddImports("RuleKeeper.Sdk.Abstractions.Nodes");

        foreach (var ns in _script.Usings)
        {
            options = options.AddImports(ns);
        }

        return options;
    }

    /// <summary>
    /// Clear the script cache.
    /// </summary>
    public static void ClearCache()
    {
        _scriptCache.Clear();
    }
}

/// <summary>
/// Global variables and helpers available in script validators.
/// </summary>
public class ScriptGlobals
{
    private readonly ValidationContext _context;
    private readonly string _validatorId;
    private readonly string _validatorName;
    private readonly string? _defaultMessage;
    private readonly List<Violation> _violations = new();

    public ScriptGlobals(ValidationContext context, string validatorId, string validatorName, string? defaultMessage)
    {
        _context = context;
        _validatorId = validatorId;
        _validatorName = validatorName;
        _defaultMessage = defaultMessage;
    }

    /// <summary>
    /// The validation context.
    /// </summary>
    public ValidationContext Context => _context;

    /// <summary>
    /// Rule parameters from YAML configuration.
    /// </summary>
    public Dictionary<string, object> Parameters => _context.Parameters;

    /// <summary>
    /// The source text being analyzed.
    /// </summary>
    public string SourceText => _context.SourceText;

    /// <summary>
    /// The file path being analyzed.
    /// </summary>
    public string FilePath => _context.FilePath;

    /// <summary>
    /// The programming language.
    /// </summary>
    public Language Language => _context.Language;

    /// <summary>
    /// Source text split into lines.
    /// </summary>
    public string[] Lines => _context.Lines;

    /// <summary>
    /// Total number of lines.
    /// </summary>
    public int LineCount => _context.LineCount;

    /// <summary>
    /// The unified AST root node.
    /// </summary>
    public IUnifiedSyntaxNode? Root => _context.UnifiedRoot;

    /// <summary>
    /// Get all method nodes.
    /// </summary>
    public IEnumerable<Sdk.Abstractions.Nodes.IMethodNode> GetMethods()
    {
        if (Root == null) yield break;

        foreach (var node in Root.Descendants())
        {
            if (node is Sdk.Abstractions.Nodes.IMethodNode method)
                yield return method;
        }
    }

    /// <summary>
    /// Get all class nodes.
    /// </summary>
    public IEnumerable<Sdk.Abstractions.Nodes.IClassNode> GetClasses()
    {
        if (Root == null) yield break;

        foreach (var node in Root.Descendants())
        {
            if (node is Sdk.Abstractions.Nodes.IClassNode classNode)
                yield return classNode;
        }
    }

    /// <summary>
    /// Get all nodes of a specific kind.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetNodesOfKind(UnifiedSyntaxKind kind)
    {
        if (Root == null) return Enumerable.Empty<IUnifiedSyntaxNode>();
        return Root.DescendantsOfKind(kind);
    }

    /// <summary>
    /// Get all nodes matching a predicate.
    /// </summary>
    public IEnumerable<IUnifiedSyntaxNode> GetNodesWhere(Func<IUnifiedSyntaxNode, bool> predicate)
    {
        if (Root == null) return Enumerable.Empty<IUnifiedSyntaxNode>();
        return Root.Descendants().Where(predicate);
    }

    /// <summary>
    /// Get a parameter value with default.
    /// </summary>
    public T GetParameter<T>(string key, T defaultValue)
    {
        if (Parameters.TryGetValue(key, out var value))
        {
            if (value is T typed)
                return typed;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Create a violation at a specific location.
    /// </summary>
    public Violation CreateViolation(IUnifiedSyntaxNode node, string message)
    {
        var location = node.Location;
        return new Violation
        {
            RuleId = _context.RuleId ?? _validatorId,
            RuleName = _context.RuleName ?? _validatorName,
            Message = message,
            Severity = _context.Severity,
            FilePath = _context.FilePath,
            StartLine = location.StartLine,
            StartColumn = location.StartColumn,
            EndLine = location.EndLine,
            EndColumn = location.EndColumn,
            FixHint = _context.FixHint,
            CodeSnippet = _context.GetLine(location.StartLine)?.TrimEnd('\r', '\n')
        };
    }

    /// <summary>
    /// Create a violation at a line/column location.
    /// </summary>
    public Violation CreateViolation(int line, int column, string message)
    {
        return new Violation
        {
            RuleId = _context.RuleId ?? _validatorId,
            RuleName = _context.RuleName ?? _validatorName,
            Message = message,
            Severity = _context.Severity,
            FilePath = _context.FilePath,
            StartLine = line,
            StartColumn = column,
            EndLine = line,
            EndColumn = column,
            FixHint = _context.FixHint,
            CodeSnippet = _context.GetLine(line)?.TrimEnd('\r', '\n')
        };
    }

    /// <summary>
    /// Create a violation with default location.
    /// </summary>
    public Violation CreateViolation(string message)
    {
        return CreateViolation(1, 1, message);
    }

    /// <summary>
    /// Add a violation to the internal list.
    /// </summary>
    public void AddViolation(Violation violation)
    {
        _violations.Add(violation);
    }

    /// <summary>
    /// Add a violation at a node location.
    /// </summary>
    public void AddViolation(IUnifiedSyntaxNode node, string message)
    {
        _violations.Add(CreateViolation(node, message));
    }

    /// <summary>
    /// Get all added violations.
    /// </summary>
    public IEnumerable<Violation> GetViolations()
    {
        return _violations;
    }

    /// <summary>
    /// Calculate cyclomatic complexity of a method.
    /// </summary>
    public int CalculateComplexity(Sdk.Abstractions.Nodes.IMethodNode method)
    {
        var complexity = 1; // Base complexity

        var node = method as IUnifiedSyntaxNode;
        if (node == null) return complexity;

        foreach (var descendant in node.Descendants())
        {
            switch (descendant.Kind)
            {
                case UnifiedSyntaxKind.IfStatement:
                case UnifiedSyntaxKind.WhileStatement:
                case UnifiedSyntaxKind.ForStatement:
                case UnifiedSyntaxKind.ForEachStatement:
                case UnifiedSyntaxKind.SwitchCase:
                case UnifiedSyntaxKind.CatchClause:
                case UnifiedSyntaxKind.ConditionalExpression:
                    complexity++;
                    break;
                case UnifiedSyntaxKind.BinaryExpression:
                    // Check for && and ||
                    var text = descendant.Text;
                    if (text != null && (text.Contains("&&") || text.Contains("||") ||
                                         text.Contains(" and ") || text.Contains(" or ")))
                    {
                        complexity++;
                    }
                    break;
            }
        }

        return complexity;
    }

    /// <summary>
    /// Get line count for a node (excluding blank lines and comments).
    /// </summary>
    public int GetLineCount(IUnifiedSyntaxNode node)
    {
        var location = node.Location;
        var count = 0;

        for (var i = location.StartLine; i <= location.EndLine && i <= LineCount; i++)
        {
            var line = _context.GetLine(i);
            if (line != null && !string.IsNullOrWhiteSpace(line))
            {
                var trimmed = line.Trim();
                // Skip comment-only lines
                if (!trimmed.StartsWith("//") && !trimmed.StartsWith("#") &&
                    !trimmed.StartsWith("/*") && !trimmed.StartsWith("*"))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
