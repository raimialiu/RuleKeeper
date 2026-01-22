using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Core.Languages;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Core.Analysis;

/// <summary>
/// Loads source files for multi-language analysis.
/// </summary>
public class UnifiedWorkspaceLoader : IDisposable
{
    private readonly LanguageAdapterRegistry _adapterRegistry;
    private readonly RoslynWorkspaceLoader _roslynLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedWorkspaceLoader"/> class.
    /// </summary>
    /// <param name="adapterRegistry">The language adapter registry.</param>
    public UnifiedWorkspaceLoader(LanguageAdapterRegistry? adapterRegistry = null)
    {
        _adapterRegistry = adapterRegistry ?? LanguageAdapterRegistry.Instance;
        _roslynLoader = new RoslynWorkspaceLoader();
    }

    /// <summary>
    /// Loads files from the specified path for multi-language analysis.
    /// </summary>
    /// <param name="path">The path to load (file, directory, project, or solution).</param>
    /// <param name="config">The scan configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of unified analysis units.</returns>
    public async Task<List<UnifiedFileAnalysisUnit>> LoadFilesAsync(
        string path,
        ScanConfig config,
        CancellationToken cancellationToken = default)
    {
        var units = new List<UnifiedFileAnalysisUnit>();

        // Check if path is a C# solution or project
        if (Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            // Use Roslyn for C# projects/solutions
            if (config.Languages.Contains(Language.CSharp))
            {
                var roslynUnits = await _roslynLoader.LoadFilesAsync(path, config, cancellationToken);
                foreach (var unit in roslynUnits)
                {
                    units.Add(new UnifiedFileAnalysisUnit
                    {
                        FilePath = unit.FilePath,
                        Language = Language.CSharp,
                        SourceText = unit.SyntaxTree.GetText().ToString(),
                        NativeUnit = unit
                    });
                }
            }
            return units;
        }

        // For directories and files, use pattern matching for all configured languages
        if (File.Exists(path))
        {
            var unit = await LoadSingleFileAsync(path, config, cancellationToken);
            if (unit != null)
            {
                units.Add(unit);
            }
            return units;
        }

        if (Directory.Exists(path))
        {
            var files = DiscoverFiles(path, config);
            var csharpFiles = new List<string>();
            var otherFiles = new Dictionary<Language, List<string>>();

            foreach (var file in files)
            {
                var language = _adapterRegistry.GetLanguageForFile(file);
                if (language == null)
                {
                    // Try to infer from extension
                    language = InferLanguageFromExtension(file);
                }

                if (language == null || !config.Languages.Contains(language.Value))
                    continue;

                if (language == Language.CSharp)
                {
                    csharpFiles.Add(file);
                }
                else
                {
                    if (!otherFiles.ContainsKey(language.Value))
                    {
                        otherFiles[language.Value] = new List<string>();
                    }
                    otherFiles[language.Value].Add(file);
                }
            }

            // Load C# files using Roslyn
            if (csharpFiles.Count > 0)
            {
                var roslynUnits = await _roslynLoader.LoadFilesAsync(path, config, cancellationToken);
                foreach (var unit in roslynUnits.Where(u => csharpFiles.Contains(u.FilePath)))
                {
                    units.Add(new UnifiedFileAnalysisUnit
                    {
                        FilePath = unit.FilePath,
                        Language = Language.CSharp,
                        SourceText = unit.SyntaxTree.GetText().ToString(),
                        NativeUnit = unit
                    });
                }
            }

            // Load other language files using their adapters
            foreach (var (language, languageFiles) in otherFiles)
            {
                var adapter = _adapterRegistry.GetAdapter(language);
                if (adapter == null)
                    continue;

                foreach (var file in languageFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var sourceText = await File.ReadAllTextAsync(file, cancellationToken);
                        var root = await adapter.ParseAsync(sourceText, file, cancellationToken);

                        units.Add(new UnifiedFileAnalysisUnit
                        {
                            FilePath = file,
                            Language = language,
                            SourceText = sourceText,
                            UnifiedRoot = root
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.Error.WriteLine($"Error parsing {file}: {ex.Message}");
                    }
                }
            }
        }

        return units;
    }

    /// <summary>
    /// Loads a single file for analysis.
    /// </summary>
    private async Task<UnifiedFileAnalysisUnit?> LoadSingleFileAsync(
        string filePath,
        ScanConfig config,
        CancellationToken cancellationToken)
    {
        var language = _adapterRegistry.GetLanguageForFile(filePath) ?? InferLanguageFromExtension(filePath);
        if (language == null || !config.Languages.Contains(language.Value))
            return null;

        var sourceText = await File.ReadAllTextAsync(filePath, cancellationToken);

        if (language == Language.CSharp)
        {
            var roslynUnits = await _roslynLoader.LoadFilesAsync(filePath, config, cancellationToken);
            if (roslynUnits.Count > 0)
            {
                return new UnifiedFileAnalysisUnit
                {
                    FilePath = filePath,
                    Language = Language.CSharp,
                    SourceText = sourceText,
                    NativeUnit = roslynUnits[0]
                };
            }
        }

        var adapter = _adapterRegistry.GetAdapter(language.Value);
        if (adapter != null)
        {
            var root = await adapter.ParseAsync(sourceText, filePath, cancellationToken);
            return new UnifiedFileAnalysisUnit
            {
                FilePath = filePath,
                Language = language.Value,
                SourceText = sourceText,
                UnifiedRoot = root
            };
        }

        return null;
    }

    /// <summary>
    /// Discovers files matching the configured patterns.
    /// </summary>
    private List<string> DiscoverFiles(string directory, ScanConfig config)
    {
        var matcher = new Matcher();

        var includePatterns = config.GetAllIncludePatterns();
        var excludePatterns = config.GetAllExcludePatterns();

        foreach (var pattern in includePatterns)
        {
            matcher.AddInclude(pattern);
        }

        foreach (var pattern in excludePatterns)
        {
            matcher.AddExclude(pattern);
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directory)));
        return result.Files
            .Select(f => Path.Combine(directory, f.Path))
            .Where(f => File.Exists(f))
            .Where(f => new FileInfo(f).Length <= config.MaxFileSize)
            .ToList();
    }

    /// <summary>
    /// Infers the language from a file extension.
    /// </summary>
    private Language? InferLanguageFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => Language.CSharp,
            ".py" or ".pyw" => Language.Python,
            ".js" or ".jsx" or ".mjs" or ".cjs" => Language.JavaScript,
            ".ts" or ".tsx" or ".mts" or ".cts" => Language.TypeScript,
            ".java" => Language.Java,
            ".go" => Language.Go,
            ".fs" or ".fsx" or ".fsi" => Language.FSharp,
            ".vb" => Language.VisualBasic,
            _ => null
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _roslynLoader.Dispose();
    }
}

/// <summary>
/// Represents a file ready for multi-language analysis.
/// </summary>
public class UnifiedFileAnalysisUnit
{
    /// <summary>
    /// The file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The programming language of the file.
    /// </summary>
    public required Language Language { get; init; }

    /// <summary>
    /// The source text of the file.
    /// </summary>
    public required string SourceText { get; init; }

    /// <summary>
    /// The unified syntax tree root for non-C# files.
    /// </summary>
    public IUnifiedSyntaxNode? UnifiedRoot { get; init; }

    /// <summary>
    /// The native analysis unit (e.g., Roslyn FileAnalysisUnit for C#).
    /// </summary>
    public object? NativeUnit { get; init; }
}
