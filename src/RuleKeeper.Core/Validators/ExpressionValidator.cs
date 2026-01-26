using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Validator that uses C# expressions for validation logic.
/// </summary>
public class ExpressionValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _name;
    private readonly string? _description;
    private readonly ExpressionConfig _expression;
    private readonly string? _defaultMessage;

    private static readonly ConcurrentDictionary<string, Script<bool>> _conditionCache = new();
    private static readonly ConcurrentDictionary<string, Script<string>> _messageCache = new();
    private static readonly ConcurrentDictionary<string, Script<SeverityLevel>> _severityCache = new();

    public override string ValidatorId => _validatorId;
    public override string Name => _name;
    public override string? Description => _description;

    public ExpressionValidator(
        string validatorId,
        string name,
        string? description,
        ExpressionConfig expression,
        string? defaultMessage = null)
    {
        _validatorId = validatorId;
        _name = name;
        _description = description;
        _expression = expression;
        _defaultMessage = defaultMessage;
    }

    public override async Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();

        if (string.IsNullOrEmpty(_expression.Condition))
            return violations;

        try
        {
            // Get nodes to evaluate against
            var nodes = GetNodesToEvaluate(context);

            foreach (var node in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var globals = CreateGlobals(context, node);
                var matched = await EvaluateConditionAsync(_expression.Condition, globals, cancellationToken);

                if (matched)
                {
                    var message = await GetMessageAsync(context, globals, cancellationToken);
                    var severity = await GetSeverityAsync(context, globals, cancellationToken);

                    violations.Add(CreateViolationFromNode(context, node, message, severity));
                }
            }
        }
        catch (CompilationErrorException ex)
        {
            // Report compilation error as a violation
            violations.Add(CreateViolation(
                context,
                $"Expression compilation error: {string.Join(", ", ex.Diagnostics.Select(d => d.GetMessage()))}",
                1, 1));
        }
        catch (Exception ex)
        {
            violations.Add(CreateViolation(
                context,
                $"Expression evaluation error: {ex.Message}",
                1, 1));
        }

        return violations;
    }

    private IEnumerable<IUnifiedSyntaxNode?> GetNodesToEvaluate(ValidationContext context)
    {
        if (context.UnifiedRoot != null)
        {
            // Evaluate against all nodes or specific node types based on expression context
            // For simplicity, yield all nodes - more sophisticated filtering could be added
            foreach (var node in context.UnifiedRoot.Descendants())
            {
                yield return node;
            }
        }
        else
        {
            // No unified AST - evaluate once with null node
            yield return null;
        }
    }

    private async Task<bool> EvaluateConditionAsync(string condition, ExpressionGlobals globals, CancellationToken cancellationToken)
    {
        var cacheKey = _expression.Cache ? condition : null;
        Script<bool>? script = null;

        if (cacheKey != null && _conditionCache.TryGetValue(cacheKey, out script))
        {
            var result = await script.RunAsync(globals, cancellationToken);
            return result.ReturnValue;
        }

        var options = CreateScriptOptions();
        script = CSharpScript.Create<bool>(condition, options, typeof(ExpressionGlobals));

        if (cacheKey != null)
        {
            _conditionCache.TryAdd(cacheKey, script);
        }

        var runResult = await script.RunAsync(globals, cancellationToken);
        return runResult.ReturnValue;
    }

    private async Task<string> GetMessageAsync(ValidationContext context, ExpressionGlobals globals, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_expression.MessageExpression))
        {
            return context.CustomMessage ?? _defaultMessage ?? "Expression condition matched";
        }

        var cacheKey = _expression.Cache ? _expression.MessageExpression : null;
        Script<string>? script = null;

        if (cacheKey != null && _messageCache.TryGetValue(cacheKey, out script))
        {
            var result = await script.RunAsync(globals, cancellationToken);
            return result.ReturnValue ?? _defaultMessage ?? "Expression condition matched";
        }

        var options = CreateScriptOptions();
        script = CSharpScript.Create<string>(_expression.MessageExpression, options, typeof(ExpressionGlobals));

        if (cacheKey != null)
        {
            _messageCache.TryAdd(cacheKey, script);
        }

        var runResult = await script.RunAsync(globals, cancellationToken);
        return runResult.ReturnValue ?? _defaultMessage ?? "Expression condition matched";
    }

    private async Task<SeverityLevel> GetSeverityAsync(ValidationContext context, ExpressionGlobals globals, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_expression.SeverityExpression))
        {
            return context.Severity;
        }

        var cacheKey = _expression.Cache ? _expression.SeverityExpression : null;
        Script<SeverityLevel>? script = null;

        if (cacheKey != null && _severityCache.TryGetValue(cacheKey, out script))
        {
            var result = await script.RunAsync(globals, cancellationToken);
            return result.ReturnValue;
        }

        var options = CreateScriptOptions();
        script = CSharpScript.Create<SeverityLevel>(_expression.SeverityExpression, options, typeof(ExpressionGlobals));

        if (cacheKey != null)
        {
            _severityCache.TryAdd(cacheKey, script);
        }

        var runResult = await script.RunAsync(globals, cancellationToken);
        return runResult.ReturnValue;
    }

    private ScriptOptions CreateScriptOptions()
    {
        var options = ScriptOptions.Default
            .AddReferences(typeof(object).Assembly)
            .AddReferences(typeof(Enumerable).Assembly)
            .AddReferences(typeof(IUnifiedSyntaxNode).Assembly)
            .AddReferences(typeof(Violation).Assembly)
            .AddImports("System")
            .AddImports("System.Linq")
            .AddImports("System.Collections.Generic")
            .AddImports("RuleKeeper.Sdk")
            .AddImports("RuleKeeper.Sdk.Abstractions")
            .AddImports("RuleKeeper.Sdk.Abstractions.Nodes");

        foreach (var ns in _expression.Usings)
        {
            options = options.AddImports(ns);
        }

        return options;
    }

    private ExpressionGlobals CreateGlobals(ValidationContext context, IUnifiedSyntaxNode? node)
    {
        return new ExpressionGlobals
        {
            Node = node,
            Context = context,
            Parameters = context.Parameters,
            SourceText = context.SourceText,
            FilePath = context.FilePath,
            Language = context.Language,
            Lines = context.Lines,
            LineCount = context.LineCount
        };
    }

    private Violation CreateViolationFromNode(ValidationContext context, IUnifiedSyntaxNode? node, string message, SeverityLevel severity)
    {
        if (node != null)
        {
            var location = node.Location;
            return new Violation
            {
                RuleId = context.RuleId ?? ValidatorId,
                RuleName = context.RuleName ?? Name,
                Message = message,
                Severity = severity,
                FilePath = context.FilePath,
                StartLine = location.StartLine,
                StartColumn = location.StartColumn,
                EndLine = location.EndLine,
                EndColumn = location.EndColumn,
                FixHint = context.FixHint,
                CodeSnippet = context.GetLine(location.StartLine)?.TrimEnd('\r', '\n')
            };
        }

        return CreateViolation(context, message, 1, 1);
    }

    /// <summary>
    /// Clear the expression cache.
    /// </summary>
    public static void ClearCache()
    {
        _conditionCache.Clear();
        _messageCache.Clear();
        _severityCache.Clear();
    }
}

/// <summary>
/// Global variables available in expression scripts.
/// </summary>
public class ExpressionGlobals
{
    /// <summary>
    /// The current AST node being evaluated.
    /// </summary>
    public IUnifiedSyntaxNode? Node { get; init; }

    /// <summary>
    /// The validation context.
    /// </summary>
    public ValidationContext Context { get; init; } = null!;

    /// <summary>
    /// Rule parameters from YAML configuration.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// The source text being analyzed.
    /// </summary>
    public string SourceText { get; init; } = "";

    /// <summary>
    /// The file path being analyzed.
    /// </summary>
    public string FilePath { get; init; } = "";

    /// <summary>
    /// The programming language.
    /// </summary>
    public Language Language { get; init; }

    /// <summary>
    /// Source text split into lines.
    /// </summary>
    public string[] Lines { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Total number of lines.
    /// </summary>
    public int LineCount { get; init; }

    /// <summary>
    /// Get line count excluding blank lines.
    /// </summary>
    public int GetNonBlankLineCount()
    {
        return Lines.Count(line => !string.IsNullOrWhiteSpace(line));
    }

    /// <summary>
    /// Check if node is of a specific type.
    /// </summary>
    public bool IsNodeKind(string kind)
    {
        return Node?.Kind.ToString().Equals(kind, StringComparison.OrdinalIgnoreCase) == true;
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
}
