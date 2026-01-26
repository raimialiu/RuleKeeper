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