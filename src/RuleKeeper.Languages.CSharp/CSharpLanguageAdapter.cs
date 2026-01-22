using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Languages.CSharp;

/// <summary>
/// Language adapter for C# using Roslyn.
/// </summary>
public class CSharpLanguageAdapter : ILanguageAdapter
{
    /// <inheritdoc />
    public Language Language => Language.CSharp;

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => new[] { ".cs" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultIncludePatterns => new[] { "**/*.cs" };

    /// <inheritdoc />
    public IEnumerable<string> DefaultExcludePatterns => new[]
    {
        "**/obj/**",
        "**/bin/**",
        "**/*.Designer.cs",
        "**/*.g.cs",
        "**/*.generated.cs"
    };

    /// <inheritdoc />
    public Task<IUnifiedSyntaxNode> ParseAsync(string source, string filePath, CancellationToken cancellationToken = default)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath, cancellationToken: cancellationToken);
        var root = syntaxTree.GetRoot(cancellationToken);

        IUnifiedSyntaxNode unifiedRoot = new CSharpUnifiedNode(root, null, filePath);
        return Task.FromResult(unifiedRoot);
    }

    /// <inheritdoc />
    public Task<ITypeResolver?> CreateTypeResolverAsync(IEnumerable<string> files, CancellationToken cancellationToken = default)
    {
        // For semantic analysis, we need a compilation
        var syntaxTrees = new List<SyntaxTree>();
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                var source = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(source, path: file, cancellationToken: cancellationToken);
                syntaxTrees.Add(tree);
            }
        }

        if (syntaxTrees.Count == 0)
            return Task.FromResult<ITypeResolver?>(null);

        var compilation = CSharpCompilation.Create(
            "Analysis",
            syntaxTrees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ITypeResolver resolver = new CSharpTypeResolver(compilation);
        return Task.FromResult<ITypeResolver?>(resolver);
    }

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object? GetNativeContext(IUnifiedSyntaxNode root, string source, string filePath, ITypeResolver? typeResolver)
    {
        if (root is CSharpUnifiedNode csNode && csNode.NativeNode is SyntaxNode syntaxNode)
        {
            var syntaxTree = syntaxNode.SyntaxTree;
            SemanticModel? semanticModel = null;
            CSharpCompilation? compilation = null;

            if (typeResolver is CSharpTypeResolver csResolver)
            {
                compilation = csResolver.Compilation;
                semanticModel = compilation.GetSemanticModel(syntaxTree);
            }

            return new CSharpNativeContext
            {
                SyntaxTree = syntaxTree,
                SemanticModel = semanticModel,
                Compilation = compilation,
                FilePath = filePath
            };
        }

        return null;
    }
}

/// <summary>
/// Native C# context containing Roslyn-specific objects.
/// </summary>
public class CSharpNativeContext
{
    /// <summary>
    /// The Roslyn syntax tree.
    /// </summary>
    public required SyntaxTree SyntaxTree { get; init; }

    /// <summary>
    /// The semantic model, if available.
    /// </summary>
    public SemanticModel? SemanticModel { get; init; }

    /// <summary>
    /// The compilation, if available.
    /// </summary>
    public CSharpCompilation? Compilation { get; init; }

    /// <summary>
    /// The file path.
    /// </summary>
    public required string FilePath { get; init; }
}
