using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using RuleKeeper.Core.Configuration.Models;

namespace RuleKeeper.Core.Analysis;

/// <summary>
/// Loads C# source files into Roslyn for analysis.
/// </summary>
public class RoslynWorkspaceLoader : IDisposable
{
    private static bool _msbuildLocatorRegistered;
    private static readonly object _lockObject = new();
    private MSBuildWorkspace? _workspace;

    /// <summary>
    /// Ensures MSBuild is registered for workspace loading.
    /// </summary>
    public static void EnsureMSBuildRegistered()
    {
        lock (_lockObject)
        {
            if (!_msbuildLocatorRegistered && MSBuildLocator.CanRegister)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (instances.Count > 0)
                {
                    MSBuildLocator.RegisterInstance(instances.OrderByDescending(i => i.Version).First());
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                }
                _msbuildLocatorRegistered = true;
            }
        }
    }

    /// <summary>
    /// Loads a solution file and returns all C# projects.
    /// </summary>
    public async Task<List<ProjectInfo>> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        EnsureMSBuildRegistered();
        _workspace = MSBuildWorkspace.Create();

        var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
        var projectInfos = new List<ProjectInfo>();

        foreach (var project in solution.Projects)
        {
            if (project.Language != LanguageNames.CSharp)
                continue;

            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation == null)
                continue;

            projectInfos.Add(new ProjectInfo
            {
                Name = project.Name,
                FilePath = project.FilePath ?? "",
                Compilation = (CSharpCompilation)compilation,
                Documents = project.Documents.ToList()
            });
        }

        return projectInfos;
    }

    /// <summary>
    /// Loads a project file.
    /// </summary>
    public async Task<ProjectInfo?> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        EnsureMSBuildRegistered();
        _workspace = MSBuildWorkspace.Create();

        var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
        if (project.Language != LanguageNames.CSharp)
            return null;

        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
            return null;

        return new ProjectInfo
        {
            Name = project.Name,
            FilePath = project.FilePath ?? "",
            Compilation = (CSharpCompilation)compilation,
            Documents = project.Documents.ToList()
        };
    }

    /// <summary>
    /// Loads individual C# files without a project context.
    /// </summary>
    public async Task<List<FileAnalysisUnit>> LoadFilesAsync(
        string path,
        ScanConfig config,
        CancellationToken cancellationToken = default)
    {
        var files = new List<string>();

        if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            files.Add(Path.GetFullPath(path));
        }
        else if (File.Exists(path) && (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                                       path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            // For solution/project files, use MSBuild workspace
            return await LoadFromProjectOrSolutionAsync(path, config, cancellationToken);
        }
        else if (Directory.Exists(path))
        {
            files = FindCSharpFiles(path, config);
        }
        else
        {
            throw new ArgumentException($"Path not found: {path}");
        }

        return await ParseFilesAsync(files, cancellationToken);
    }

    /// <summary>
    /// Loads files from a project or solution with full semantic information.
    /// </summary>
    private async Task<List<FileAnalysisUnit>> LoadFromProjectOrSolutionAsync(
        string path,
        ScanConfig config,
        CancellationToken cancellationToken)
    {
        var units = new List<FileAnalysisUnit>();

        if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var projects = await LoadSolutionAsync(path, cancellationToken);
            foreach (var project in projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == null)
                        continue;

                    if (IsExcluded(document.FilePath, config.Exclude))
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

                    if (syntaxTree != null)
                    {
                        units.Add(new FileAnalysisUnit
                        {
                            FilePath = document.FilePath,
                            SyntaxTree = syntaxTree,
                            SemanticModel = semanticModel,
                            Compilation = project.Compilation
                        });
                    }
                }
            }
        }
        else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var project = await LoadProjectAsync(path, cancellationToken);
            if (project != null)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == null)
                        continue;

                    if (IsExcluded(document.FilePath, config.Exclude))
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

                    if (syntaxTree != null)
                    {
                        units.Add(new FileAnalysisUnit
                        {
                            FilePath = document.FilePath,
                            SyntaxTree = syntaxTree,
                            SemanticModel = semanticModel,
                            Compilation = project.Compilation
                        });
                    }
                }
            }
        }

        return units;
    }

    /// <summary>
    /// Parses C# files without a project context (syntax only, no semantic model).
    /// </summary>
    private async Task<List<FileAnalysisUnit>> ParseFilesAsync(
        List<string> files,
        CancellationToken cancellationToken)
    {
        var units = new List<FileAnalysisUnit>();

        // Create a simple compilation for basic semantic analysis
        var syntaxTrees = new List<SyntaxTree>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceText = await File.ReadAllTextAsync(file, cancellationToken);
            var syntaxTree = CSharpSyntaxTree.ParseText(
                sourceText,
                CSharpParseOptions.Default,
                file);

            syntaxTrees.Add(syntaxTree);
        }

        // Create a compilation with basic references for semantic analysis
        var compilation = CSharpCompilation.Create(
            "Analysis",
            syntaxTrees,
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        foreach (var syntaxTree in syntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            units.Add(new FileAnalysisUnit
            {
                FilePath = syntaxTree.FilePath,
                SyntaxTree = syntaxTree,
                SemanticModel = semanticModel,
                Compilation = compilation
            });
        }

        return units;
    }

    /// <summary>
    /// Finds all C# files matching the scan configuration.
    /// </summary>
    private List<string> FindCSharpFiles(string basePath, ScanConfig config)
    {
        var matcher = new Matcher();

        foreach (var pattern in config.Include)
        {
            matcher.AddInclude(pattern);
        }

        foreach (var pattern in config.Exclude)
        {
            matcher.AddExclude(pattern);
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

        return result.Files
            .Select(f => Path.GetFullPath(Path.Combine(basePath, f.Path)))
            .Where(f => new FileInfo(f).Length <= config.MaxFileSize)
            .ToList();
    }

    private bool IsExcluded(string filePath, List<string> excludePatterns)
    {
        if (excludePatterns.Count == 0)
            return false;

        var matcher = new Matcher();
        foreach (var pattern in excludePatterns)
        {
            matcher.AddInclude(pattern);
        }

        var fileName = Path.GetFileName(filePath);
        return matcher.Match(fileName).HasMatches ||
               excludePatterns.Any(p => filePath.Contains(p.Replace("**", "").Replace("*", "").Trim('/')));
    }

    private static IEnumerable<MetadataReference> GetBasicReferences()
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
        };
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}

/// <summary>
/// Information about a loaded project.
/// </summary>
public class ProjectInfo
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required CSharpCompilation Compilation { get; init; }
    public required List<Document> Documents { get; init; }
}

/// <summary>
/// A single file ready for analysis.
/// </summary>
public class FileAnalysisUnit
{
    public required string FilePath { get; init; }
    public required SyntaxTree SyntaxTree { get; init; }
    public SemanticModel? SemanticModel { get; init; }
    public CSharpCompilation? Compilation { get; init; }
}
