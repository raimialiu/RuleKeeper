using RuleKeeper.Core.Analysis;
using RuleKeeper.Core.Configuration.Models;

namespace RuleKeeper.Core.Reporting;

/// <summary>
/// Interface for report generators.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// The format name this generator produces.
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Generates a report from the analysis results.
    /// </summary>
    /// <param name="report">The analysis report.</param>
    /// <param name="config">The output configuration.</param>
    /// <returns>The generated report as a string.</returns>
    string Generate(AnalysisReport report, OutputConfig config);

    /// <summary>
    /// Writes the report to a file.
    /// </summary>
    /// <param name="report">The analysis report.</param>
    /// <param name="config">The output configuration.</param>
    /// <param name="filePath">The output file path.</param>
    Task WriteToFileAsync(AnalysisReport report, OutputConfig config, string filePath);
}

/// <summary>
/// Factory for creating report generators.
/// </summary>
public static class ReportGeneratorFactory
{
    private static readonly Dictionary<string, Func<IReportGenerator>> Generators = new(StringComparer.OrdinalIgnoreCase)
    {
        ["console"] = () => new ConsoleReporter(),
        ["json"] = () => new JsonReporter(),
        ["sarif"] = () => new SarifReporter(),
        ["html"] = () => new HtmlReporter()
    };

    /// <summary>
    /// Creates a report generator for the specified format.
    /// </summary>
    public static IReportGenerator Create(string format)
    {
        if (Generators.TryGetValue(format, out var factory))
        {
            return factory();
        }

        throw new ArgumentException($"Unknown report format: {format}. Supported formats: {string.Join(", ", Generators.Keys)}");
    }

    /// <summary>
    /// Gets all supported format names.
    /// </summary>
    public static IEnumerable<string> SupportedFormats => Generators.Keys;
}
