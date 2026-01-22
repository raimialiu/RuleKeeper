namespace RuleKeeper.Sdk;

/// <summary>
/// Supported programming languages for analysis.
/// </summary>
public enum Language
{
    /// <summary>
    /// C# programming language.
    /// </summary>
    CSharp,

    /// <summary>
    /// Python programming language.
    /// </summary>
    Python,

    /// <summary>
    /// JavaScript programming language.
    /// </summary>
    JavaScript,

    /// <summary>
    /// TypeScript programming language.
    /// </summary>
    TypeScript,

    /// <summary>
    /// Java programming language.
    /// </summary>
    Java,

    /// <summary>
    /// Go programming language.
    /// </summary>
    Go,

    /// <summary>
    /// F# programming language.
    /// </summary>
    FSharp,

    /// <summary>
    /// Visual Basic .NET programming language.
    /// </summary>
    VisualBasic
}

/// <summary>
/// Extension methods for the <see cref="Language"/> enum.
/// </summary>
public static class LanguageExtensions
{
    /// <summary>
    /// Gets the display name for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The display name.</returns>
    public static string GetDisplayName(this Language language) => language switch
    {
        Language.CSharp => "C#",
        Language.Python => "Python",
        Language.JavaScript => "JavaScript",
        Language.TypeScript => "TypeScript",
        Language.Java => "Java",
        Language.Go => "Go",
        Language.FSharp => "F#",
        Language.VisualBasic => "Visual Basic",
        _ => language.ToString()
    };

    /// <summary>
    /// Gets the file extensions for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The file extensions.</returns>
    public static IEnumerable<string> GetFileExtensions(this Language language) => language switch
    {
        Language.CSharp => new[] { ".cs" },
        Language.Python => new[] { ".py", ".pyw" },
        Language.JavaScript => new[] { ".js", ".jsx", ".mjs", ".cjs" },
        Language.TypeScript => new[] { ".ts", ".tsx", ".mts", ".cts" },
        Language.Java => new[] { ".java" },
        Language.Go => new[] { ".go" },
        Language.FSharp => new[] { ".fs", ".fsx", ".fsi" },
        Language.VisualBasic => new[] { ".vb" },
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// Gets the default include patterns for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The default include patterns.</returns>
    public static IEnumerable<string> GetDefaultIncludePatterns(this Language language) => language switch
    {
        Language.CSharp => new[] { "**/*.cs" },
        Language.Python => new[] { "**/*.py" },
        Language.JavaScript => new[] { "**/*.js", "**/*.jsx", "**/*.mjs" },
        Language.TypeScript => new[] { "**/*.ts", "**/*.tsx" },
        Language.Java => new[] { "**/*.java" },
        Language.Go => new[] { "**/*.go" },
        Language.FSharp => new[] { "**/*.fs", "**/*.fsx" },
        Language.VisualBasic => new[] { "**/*.vb" },
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// Gets the default exclude patterns for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The default exclude patterns.</returns>
    public static IEnumerable<string> GetDefaultExcludePatterns(this Language language) => language switch
    {
        Language.CSharp => new[] { "**/obj/**", "**/bin/**", "**/*.Designer.cs", "**/*.g.cs", "**/*.generated.cs" },
        Language.Python => new[] { "**/venv/**", "**/__pycache__/**", "**/.env/**", "**/env/**", "**/.venv/**" },
        Language.JavaScript => new[] { "**/node_modules/**", "**/dist/**", "**/build/**", "**/*.min.js" },
        Language.TypeScript => new[] { "**/node_modules/**", "**/dist/**", "**/build/**", "**/*.d.ts" },
        Language.Java => new[] { "**/target/**", "**/build/**", "**/*.class" },
        Language.Go => new[] { "**/vendor/**", "**/bin/**" },
        Language.FSharp => new[] { "**/obj/**", "**/bin/**" },
        Language.VisualBasic => new[] { "**/obj/**", "**/bin/**" },
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// Determines if the language uses ANTLR for parsing.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>True if the language uses ANTLR parsing.</returns>
    public static bool UsesAntlr(this Language language) => language switch
    {
        Language.CSharp => false, // Uses Roslyn
        Language.FSharp => false, // Uses FSharp.Compiler.Service
        Language.VisualBasic => false, // Uses Roslyn
        _ => true // All other languages use ANTLR
    };
}
